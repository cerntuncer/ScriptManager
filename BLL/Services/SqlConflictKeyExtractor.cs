using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace BLL.Services;

/// <summary>
/// T-SQL metninden conflict tespiti için yapısal anahtarlar çıkarır.
///
/// Tespit edilen conflict tipleri:
/// ┌──────────────────────────────────────────────────────────────────┐
/// │  Tip          │ Örnek SQL                                        │
/// ├──────────────────────────────────────────────────────────────────┤
/// │  Record       │ WHERE ID = 5  /  WHERE UserId IN (1,2,3)         │
/// │               │ Herhangi bir "*Id" veya "*ID" kolonu desteklenir │
/// ├──────────────────────────────────────────────────────────────────┤
/// │  Table DDL    │ ALTER TABLE Users ...                            │
/// │               │ CREATE TABLE Orders ...                          │
/// │               │ DROP TABLE Logs                                  │
/// │               │ TRUNCATE TABLE Sessions                          │
/// ├──────────────────────────────────────────────────────────────────┤
/// │  Object DDL   │ CREATE OR ALTER PROCEDURE GetUsers ...           │
/// │               │ ALTER VIEW UserSummary ...                       │
/// │               │ CREATE FUNCTION dbo.CalcTotal ...                │
/// ├──────────────────────────────────────────────────────────────────┤
/// │  DML (genel)  │ INSERT INTO / UPDATE / DELETE / MERGE            │
/// │               │ Kayıt bazlı eşleşme yoksa tablo DDL ile çakışır │
/// └──────────────────────────────────────────────────────────────────┘
///
/// Birincil yöntem: ScriptDom AST ziyaretçisi (yapısal, doğru).
/// Parse başarısız olursa: regex tabanlı fallback otomatik devreye girer.
/// </summary>
public static class SqlConflictKeyExtractor
{
    private static readonly TSqlParser Parser = new TSql160Parser(false);

    /// <summary>Tek SQL metninden conflict key'leri çıkarır.</summary>
    public static HashSet<ConflictKey> Extract(string? sql)
    {
        var result = new HashSet<ConflictKey>();
        if (string.IsNullOrWhiteSpace(sql)) return result;

        // Birincil yol: ScriptDom AST
        try
        {
            using var reader = new StringReader(sql);
            var fragment = Parser.Parse(reader, out var errors);
            if (errors == null || errors.Count == 0)
            {
                var visitor = new ConflictKeyVisitor();
                fragment.Accept(visitor);
                foreach (var k in visitor.Keys)
                    result.Add(k);
            }
        }
        catch
        {
            // parse exception → regex fallback ile devam
        }

        // Fallback: regex (ScriptDom'un atladığı edge-case'ler + parse hatası durumu)
        foreach (var k in RegexFallback.Extract(sql))
            result.Add(k);

        return result;
    }

    /// <summary>SqlScript ve RollbackScript'in birleşik key setini döner.</summary>
    public static HashSet<ConflictKey> ExtractFromScript(string? sqlScript, string? rollbackScript)
    {
        var result = Extract(sqlScript);
        foreach (var k in Extract(rollbackScript))
            result.Add(k);
        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ScriptDom AST Ziyaretçisi
    // ═══════════════════════════════════════════════════════════════════════

    private sealed class ConflictKeyVisitor : TSqlFragmentVisitor
    {
        public readonly List<ConflictKey> Keys = [];

        // ── DML ────────────────────────────────────────────────────────────

        public override void Visit(InsertStatement node)
        {
            var tbl = TargetName(node.InsertSpecification?.Target);
            if (tbl != null) Keys.Add(ConflictKey.ForDml(tbl));
        }

        public override void Visit(UpdateStatement node)
        {
            var tbl = TargetName(node.UpdateSpecification?.Target);
            if (tbl == null) return;
            Keys.Add(ConflictKey.ForDml(tbl));
            ExtractWhereIds(node.UpdateSpecification?.WhereClause);
        }

        public override void Visit(DeleteStatement node)
        {
            var tbl = TargetName(node.DeleteSpecification?.Target);
            if (tbl == null) return;
            Keys.Add(ConflictKey.ForDml(tbl));
            ExtractWhereIds(node.DeleteSpecification?.WhereClause);
        }

        public override void Visit(MergeStatement node)
        {
            var tbl = TargetName(node.MergeSpecification?.Target);
            if (tbl != null) Keys.Add(ConflictKey.ForDml(tbl));
        }

        // ── Tablo DDL ──────────────────────────────────────────────────────

        public override void Visit(AlterTableStatement node)
        {
            var tbl = SchemaName(node.SchemaObjectName);
            if (tbl != null) Keys.Add(ConflictKey.ForTableDdl(tbl));
        }

        public override void Visit(CreateTableStatement node)
        {
            var tbl = SchemaName(node.SchemaObjectName);
            if (tbl != null) Keys.Add(ConflictKey.ForTableDdl(tbl));
        }

        public override void Visit(DropTableStatement node)
        {
            foreach (var obj in node.Objects ?? Enumerable.Empty<SchemaObjectName>())
            {
                var tbl = SchemaName(obj);
                if (tbl != null) Keys.Add(ConflictKey.ForTableDdl(tbl));
            }
        }

        public override void Visit(TruncateTableStatement node)
        {
            var tbl = SchemaName(node.TableName);
            if (tbl != null) Keys.Add(ConflictKey.ForTableDdl(tbl));
        }

        // ── Nesne DDL (Procedure / View / Function) ────────────────────────

        public override void Visit(CreateProcedureStatement node)
            => AddObjectDdl(node.ProcedureReference?.Name);

        public override void Visit(AlterProcedureStatement node)
            => AddObjectDdl(node.ProcedureReference?.Name);

        public override void Visit(CreateOrAlterProcedureStatement node)
            => AddObjectDdl(node.ProcedureReference?.Name);

        public override void Visit(CreateViewStatement node)
            => AddObjectDdl(node.SchemaObjectName);

        public override void Visit(AlterViewStatement node)
            => AddObjectDdl(node.SchemaObjectName);

        public override void Visit(CreateOrAlterViewStatement node)
            => AddObjectDdl(node.SchemaObjectName);

        public override void Visit(CreateFunctionStatement node)
            => AddObjectDdl(node.Name);

        public override void Visit(AlterFunctionStatement node)
            => AddObjectDdl(node.Name);

        public override void Visit(CreateOrAlterFunctionStatement node)
            => AddObjectDdl(node.Name);

        // ── WHERE koşulundan kayıt ID'leri ────────────────────────────────

        private void ExtractWhereIds(WhereClause? where)
        {
            if (where?.SearchCondition != null)
                VisitCondition(where.SearchCondition);
        }

        private void VisitCondition(BooleanExpression expr)
        {
            switch (expr)
            {
                case BooleanBinaryExpression bin:
                    VisitCondition(bin.FirstExpression);
                    VisitCondition(bin.SecondExpression);
                    break;

                case BooleanParenthesisExpression paren:
                    VisitCondition(paren.Expression);
                    break;

                // col = 42
                case BooleanComparisonExpression { ComparisonType: BooleanComparisonType.Equals } cmp:
                    TryRecordEquality(cmp.FirstExpression, cmp.SecondExpression);
                    TryRecordEquality(cmp.SecondExpression, cmp.FirstExpression);
                    break;

                // col IN (1, 2, 3)
                case InPredicate { NotDefined: false } inPred
                    when inPred.Expression is ColumnReferenceExpression colRef:
                {
                    var col = LastIdentifier(colRef.MultiPartIdentifier);
                    if (col != null && IsIdColumn(col))
                    {
                        foreach (var val in inPred.Values ?? Enumerable.Empty<ScalarExpression>())
                            if (val is IntegerLiteral lit)
                                Keys.Add(ConflictKey.ForRecord(col, lit.Value));
                    }
                    break;
                }
            }
        }

        private void TryRecordEquality(ScalarExpression colSide, ScalarExpression valSide)
        {
            if (colSide is ColumnReferenceExpression colRef && valSide is IntegerLiteral lit)
            {
                var col = LastIdentifier(colRef.MultiPartIdentifier);
                if (col != null && IsIdColumn(col))
                    Keys.Add(ConflictKey.ForRecord(col, lit.Value));
            }
        }

        // ── Yardımcılar ────────────────────────────────────────────────────

        private void AddObjectDdl(SchemaObjectName? name)
        {
            var n = SchemaName(name);
            if (n != null) Keys.Add(ConflictKey.ForObjectDdl(n));
        }

        private static string? TargetName(TableReference? target)
            => target is NamedTableReference namedRef ? SchemaName(namedRef.SchemaObject) : null;

        private static string? SchemaName(SchemaObjectName? name)
        {
            if (name == null) return null;
            var parts = new List<string>(2);
            if (name.SchemaIdentifier?.Value is { Length: > 0 } schema) parts.Add(schema);
            if (name.BaseIdentifier?.Value is { Length: > 0 } baseName) parts.Add(baseName);
            return parts.Count > 0 ? string.Join(".", parts) : null;
        }

        private static string? LastIdentifier(MultiPartIdentifier? mpi)
            => mpi?.Identifiers?.LastOrDefault()?.Value;

        private static bool IsIdColumn(string col) =>
            col.Equals("ID", StringComparison.OrdinalIgnoreCase)
            || (col.Length > 2 && col.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            || (col.Length > 2 && col.EndsWith("ID", StringComparison.Ordinal));
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Regex Fallback
    // ═══════════════════════════════════════════════════════════════════════

    private static class RegexFallback
    {
        private static readonly Regex RxAlterTable  = Rx(@"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxCreateTable = Rx(@"(?i)\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxDropTable   = Rx(@"(?i)\bDROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxTruncate    = Rx(@"(?i)\bTRUNCATE\s+TABLE\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxProc        = Rx(@"(?i)\b(?:CREATE|ALTER)\s+(?:OR\s+ALTER\s+)?(?:PROCEDURE|PROC)\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxFunc        = Rx(@"(?i)\b(?:CREATE|ALTER)\s+(?:OR\s+ALTER\s+)?FUNCTION\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxView        = Rx(@"(?i)\b(?:CREATE|ALTER)\s+(?:OR\s+ALTER\s+)?VIEW\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxInsert      = Rx(@"(?i)\bINSERT\s+INTO\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxUpdate      = Rx(@"(?i)\bUPDATE\s+(?<t>[\w\.\[\]""` ]+)\b");
        private static readonly Regex RxDelete      = Rx(@"(?i)\bDELETE\s+FROM\s+(?<t>[\w\.\[\]""` ]+)");
        private static readonly Regex RxMerge       = Rx(@"(?i)\bMERGE\s+(?:INTO\s+)?(?<t>[\w\.\[\]""` ]+)");

        // "UserId = 42" / "ID = 42" / "OrderId IN (1,2)"
        private static readonly Regex RxRecordEq  = Rx(@"(?i)\b(?<col>\w*[Ii][Dd])\s*=\s*(?<id>\d+)\b");
        private static readonly Regex RxRecordIn  = Rx(@"(?i)\b(?<col>\w*[Ii][Dd])\s+IN\s*\((?<ids>[0-9,\s]+)\)");

        // Gürültülü yanlış eşleşmeler (gerçek ID kolonu olmayan kelimeler)
        private static readonly HashSet<string> NoiseColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "ACID", "AVOID", "FORBID", "INVALID", "VALID", "INVALID", "REBUILD", "PERIOD"
        };

        public static IEnumerable<ConflictKey> Extract(string sql)
        {
            foreach (var k in TableDdl(sql, RxAlterTable))  yield return k;
            foreach (var k in TableDdl(sql, RxCreateTable)) yield return k;
            foreach (var k in TableDdl(sql, RxDropTable))   yield return k;
            foreach (var k in TableDdl(sql, RxTruncate))    yield return k;
            foreach (var k in ObjectDdl(sql, RxProc))       yield return k;
            foreach (var k in ObjectDdl(sql, RxFunc))       yield return k;
            foreach (var k in ObjectDdl(sql, RxView))       yield return k;
            foreach (var k in Dml(sql, RxInsert))           yield return k;
            foreach (var k in Dml(sql, RxUpdate))           yield return k;
            foreach (var k in Dml(sql, RxDelete))           yield return k;
            foreach (var k in Dml(sql, RxMerge))            yield return k;
            foreach (var k in RecordEq(sql))                yield return k;
            foreach (var k in RecordIn(sql))                yield return k;
        }

        private static IEnumerable<ConflictKey> TableDdl(string sql, Regex rx)
        {
            foreach (Match m in rx.Matches(sql))
                if (m.Groups["t"].Success)
                {
                    var name = Normalize(m.Groups["t"].Value);
                    if (!string.IsNullOrEmpty(name))
                        yield return ConflictKey.ForTableDdl(name);
                }
        }

        private static IEnumerable<ConflictKey> ObjectDdl(string sql, Regex rx)
        {
            foreach (Match m in rx.Matches(sql))
                if (m.Groups["t"].Success)
                {
                    var name = Normalize(m.Groups["t"].Value);
                    if (!string.IsNullOrEmpty(name))
                        yield return ConflictKey.ForObjectDdl(name);
                }
        }

        private static IEnumerable<ConflictKey> Dml(string sql, Regex rx)
        {
            foreach (Match m in rx.Matches(sql))
                if (m.Groups["t"].Success)
                {
                    var name = Normalize(m.Groups["t"].Value);
                    if (!string.IsNullOrEmpty(name))
                        yield return ConflictKey.ForDml(name);
                }
        }

        private static IEnumerable<ConflictKey> RecordEq(string sql)
        {
            foreach (Match m in RxRecordEq.Matches(sql))
            {
                if (!m.Groups["col"].Success || !m.Groups["id"].Success) continue;
                var col = m.Groups["col"].Value;
                if (NoiseColumns.Contains(col)) continue;
                yield return ConflictKey.ForRecord(col, m.Groups["id"].Value);
            }
        }

        private static IEnumerable<ConflictKey> RecordIn(string sql)
        {
            foreach (Match m in RxRecordIn.Matches(sql))
            {
                if (!m.Groups["col"].Success || !m.Groups["ids"].Success) continue;
                var col = m.Groups["col"].Value;
                if (NoiseColumns.Contains(col)) continue;
                foreach (var raw in m.Groups["ids"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (long.TryParse(raw, out var id))
                        yield return ConflictKey.ForRecord(col, id.ToString());
            }
        }

        private static string Normalize(string raw)
        {
            var parts = raw.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join(".", parts.Select(p => p.Trim('[', ']', '"', '`').Trim()).Where(p => p.Length > 0));
        }

        private static Regex Rx(string pattern) => new(pattern, RegexOptions.Compiled);
    }
}
