using System.Text;
using System.Text.RegularExpressions;

namespace BLL.Services;

public static class SqlReferencedColumnExtractor
{
    // INSERT INTO tableName (col1, col2, col3) VALUES (...)
    private static readonly Regex InsertColumnsRx = new(
        @"(?i)\bINSERT\s+INTO\s+(?<t>[\w\.\[\]""` ]+?)\s*\((?<cols>[^)]+)\)\s*(?:VALUES|SELECT|\n)",
        RegexOptions.Compiled);

    // UPDATE [TOP(...)] tableName SET col1 = ..., col2 = ...
    // Captures everything after SET until WHERE / FROM / JOIN / GO / semicolon / end
    private static readonly Regex UpdateSetRx = new(
        @"(?i)\bUPDATE\s+(?:TOP\s*\([^)]*\)\s+)?(?<t>[\w\.\[\]""` ]+?)\s+SET\s+(?<setclause>.+?)(?=\bWHERE\b|\bFROM\b|\bJOIN\b|\bORDER\b|\bGROUP\b|\bHAVING\b|\bGO\b|;|$)",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    // ALTER TABLE tableName ADD [COLUMN] colName
    private static readonly Regex AlterAddColumnRx = new(
        @"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]""` ]+?)\s+ADD\s+(?:COLUMN\s+)?(?<col>[\w\[\]""` ]+)",
        RegexOptions.Compiled);

    // ALTER TABLE tableName DROP COLUMN [IF EXISTS] colName
    private static readonly Regex AlterDropColumnRx = new(
        @"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]""` ]+?)\s+DROP\s+COLUMN\s+(?:IF\s+EXISTS\s+)?(?<col>[\w\[\]""` ]+)",
        RegexOptions.Compiled);

    // ALTER TABLE tableName ALTER COLUMN colName
    private static readonly Regex AlterColumnRx = new(
        @"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]""` ]+?)\s+ALTER\s+COLUMN\s+(?<col>[\w\[\]""` ]+)",
        RegexOptions.Compiled);

    private static readonly Regex BlockCommentRx = new(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);
    private static readonly Regex LineCommentRx = new(@"--[^\r\n]*", RegexOptions.Compiled);

    private static string StripComments(string sql)
    {
        sql = BlockCommentRx.Replace(sql, " ");
        return LineCommentRx.Replace(sql, " ");
    }

    private static string StripStringLiterals(string sql)
    {
        var sb = new StringBuilder(sql.Length);
        for (var i = 0; i < sql.Length;)
        {
            if (i < sql.Length - 1 && (sql[i] == 'N' || sql[i] == 'n') && sql[i + 1] == '\'')
            {
                i += 2;
                i = SkipQuoted(sql, i);
                sb.Append(' ');
                continue;
            }
            if (sql[i] == '\'')
            {
                i++;
                i = SkipQuoted(sql, i);
                sb.Append(' ');
                continue;
            }
            sb.Append(sql[i]);
            i++;
        }
        return sb.ToString();
    }

    private static int SkipQuoted(string sql, int i)
    {
        while (i < sql.Length)
        {
            if (sql[i] == '\'')
            {
                if (i + 1 < sql.Length && sql[i + 1] == '\'') { i += 2; continue; }
                return i + 1;
            }
            i++;
        }
        return i;
    }

    private static string Prepare(string sql)
    {
        sql = StripComments(sql);
        sql = StripStringLiterals(sql);
        return sql;
    }

    private static string NormalizeObjectName(string raw)
    {
        var parts = raw.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(".", parts.Select(p => p.Trim('[', ']', '"', '`')).Where(p => p.Length > 0));
    }

    private static string NormalizeColumnName(string raw) =>
        raw.Trim().Trim('[', ']', '"', '`').Trim();

    private static void AddColumn(Dictionary<string, HashSet<string>> map, string table, string col)
    {
        var t = NormalizeObjectName(table);
        var c = NormalizeColumnName(col);
        if (string.IsNullOrEmpty(t) || string.IsNullOrEmpty(c)) return;
        if (!map.TryGetValue(t, out var set))
            map[t] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add(c);
    }

    /// <summary>
    /// Extracts column references per table from SQL text.
    /// Returns a dictionary mapping normalized table names to their referenced column names.
    /// </summary>
    public static Dictionary<string, HashSet<string>> ExtractColumns(string? sql)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return map;

        sql = Prepare(sql);

        // INSERT INTO t (col1, col2, col3)
        foreach (Match m in InsertColumnsRx.Matches(sql))
        {
            var table = m.Groups["t"].Value;
            foreach (var c in m.Groups["cols"].Value.Split(','))
                AddColumn(map, table, c);
        }

        // UPDATE t SET col1 = ..., col2 = ...
        foreach (Match m in UpdateSetRx.Matches(sql))
        {
            var table = m.Groups["t"].Value;
            var setClause = m.Groups["setclause"].Value;

            // Split SET clause on commas — each segment is "col = expr"
            // Naive split (works for simple cases; nested parens in expressions may split incorrectly)
            foreach (var assignment in SplitSetClause(setClause))
            {
                var eqIdx = assignment.IndexOf('=');
                if (eqIdx > 0)
                    AddColumn(map, table, assignment[..eqIdx].Trim());
            }
        }

        // ALTER TABLE t ADD col
        foreach (Match m in AlterAddColumnRx.Matches(sql))
            AddColumn(map, m.Groups["t"].Value, m.Groups["col"].Value);

        // ALTER TABLE t DROP COLUMN col
        foreach (Match m in AlterDropColumnRx.Matches(sql))
            AddColumn(map, m.Groups["t"].Value, m.Groups["col"].Value);

        // ALTER TABLE t ALTER COLUMN col
        foreach (Match m in AlterColumnRx.Matches(sql))
            AddColumn(map, m.Groups["t"].Value, m.Groups["col"].Value);

        return map;
    }

    // Splits a SET clause on top-level commas (skips commas inside parentheses)
    private static IEnumerable<string> SplitSetClause(string setClause)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < setClause.Length; i++)
        {
            if (setClause[i] == '(') depth++;
            else if (setClause[i] == ')') depth--;
            else if (setClause[i] == ',' && depth == 0)
            {
                parts.Add(setClause[start..i].Trim());
                start = i + 1;
            }
        }
        if (start < setClause.Length)
            parts.Add(setClause[start..].Trim());
        return parts;
    }
}
