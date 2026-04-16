using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace BLL.Services;

/// <summary>
/// 1) Tek kelimelik / anlamsız batch heuristiği
/// 2) ScriptDom parse hataları
/// 3) SQL Server: <c>SET NOEXEC ON</c> — çalıştırmadan derleme; <c>NVAAARCHAR</c> gibi geçersiz türler PARSEONLY’de kaçabilir, NOEXEC yakalar.
///    Her batch kendi komutunda <c>SET NOEXEC ON;</c> + metin (havuz bağlantılarında oturum sıfırlanmasına karşı).
/// </summary>
public sealed class SqlScriptSyntaxValidator : ISqlScriptSyntaxValidator
{
    private static readonly TSqlParser Parser = new TSql160Parser(false);

    /// <summary>Tek başına geçerli sayılabilecek tek kelimelik ifadeler (migration’da nadir).</summary>
    private static readonly HashSet<string> StandaloneSingleWordOk = new(StringComparer.OrdinalIgnoreCase)
    {
        "COMMIT", "ROLLBACK", "BEGIN", "END", "RETURN", "BREAK", "CONTINUE"
    };

    private readonly string? _parseOnlyConnectionString;

    public SqlScriptSyntaxValidator(string? parseOnlyConnectionString)
    {
        _parseOnlyConnectionString = parseOnlyConnectionString;
    }

    public SqlScriptSyntaxResult Validate(string? sqlText, string labelPrefix)
    {
        var prefix = string.IsNullOrWhiteSpace(labelPrefix) ? "SQL" : labelPrefix.Trim();
        if (string.IsNullOrWhiteSpace(sqlText))
            return new SqlScriptSyntaxResult { IsValid = true, Issues = Array.Empty<SqlScriptSyntaxIssue>() };

        var batches = SplitGoBatches(sqlText).ToList();
        if (batches.Count == 0)
            return new SqlScriptSyntaxResult { IsValid = true, Issues = Array.Empty<SqlScriptSyntaxIssue>() };

        var issues = new List<SqlScriptSyntaxIssue>();

        AppendHeuristicBareBatchIssues(batches, prefix, issues);

        AppendMysqlDialectHints(batches, prefix, issues);

        var dom = ValidateWithScriptDom(batches, prefix);
        issues.AddRange(dom.Issues);

        if (!string.IsNullOrWhiteSpace(_parseOnlyConnectionString))
        {
            var server = TryValidateWithSqlServerNoExec(batches, prefix);
            if (server != null)
                issues.AddRange(server.Issues);
        }

        issues = DeduplicateIssues(issues);
        return new SqlScriptSyntaxResult { IsValid = issues.Count == 0, Issues = issues };
    }

    /// <summary>
    /// Tek satır, tek "kelime", tırnak/parantez yok → geçerli T-SQL değil (ör. rastgele string).
    /// </summary>
    private static void AppendHeuristicBareBatchIssues(
        IReadOnlyList<string> batches,
        string prefix,
        List<SqlScriptSyntaxIssue> issues)
    {
        for (var i = 0; i < batches.Count; i++)
        {
            var raw = batches[i];
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var batchNumber = i + 1;
            var t = raw.Trim();
            if (t.Contains('\n'))
                continue;

            if (t.IndexOf('(') >= 0 || t.IndexOf(';') >= 0)
                continue;
            if (t.StartsWith('\'') || t.StartsWith("N'", StringComparison.Ordinal))
                continue;
            if (t.StartsWith("/*", StringComparison.Ordinal))
                continue;

            var parts = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 1)
                continue;

            var word = parts[0];
            if (!Regex.IsMatch(word, @"^[A-Za-z_#][\w@#$]*$"))
                continue;

            if (StandaloneSingleWordOk.Contains(word))
                continue;

            issues.Add(new SqlScriptSyntaxIssue
            {
                Source = prefix,
                BatchNumber = batchNumber,
                Line = 1,
                Column = 1,
                Message =
                    "Geçerli bir T-SQL komutu görünmüyor (en az bir anahtar kelime veya ifade gerekir)."
            });
        }
    }

    /// <summary>
    /// MySQL / PostgreSQL sözdizimleri tespit edildiğinde T-SQL karşılığını öneren uyarılar üretir.
    /// </summary>
    private static void AppendMysqlDialectHints(
        IReadOnlyList<string> batches,
        string prefix,
        List<SqlScriptSyntaxIssue> issues)
    {
        // keyword → T-SQL karşılığı hint
        var hints = new (Regex Pattern, string Hint)[]
        {
            (new Regex(@"\bAUTO_INCREMENT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: AUTO_INCREMENT T-SQL'de desteklenmez. Yerine IDENTITY(1,1) kullanın. Örn: Id INT PRIMARY KEY IDENTITY(1,1)"),
            (new Regex(@"\bENGINE\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: ENGINE= T-SQL'de gerekli değildir."),
            (new Regex(@"\bDEFAULT\s+CHARSET\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: DEFAULT CHARSET T-SQL'de kullanılmaz."),
            (new Regex(@"\bUNSIGNED\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: UNSIGNED T-SQL'de desteklenmez. Negatif değerleri kısıtlamak için CHECK kısıtı kullanın."),
            (new Regex(@"\bLIMIT\s+\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: LIMIT T-SQL'de desteklenmez. Yerine TOP veya OFFSET/FETCH NEXT kullanın."),
            (new Regex(@"\bIFNULL\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: IFNULL() T-SQL'de ISNULL() veya COALESCE() ile yapılır."),
            (new Regex(@"\bGROUP_CONCAT\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "MySQL sözdizimi: GROUP_CONCAT() T-SQL'de STRING_AGG() ile yapılır."),
        };

        for (var i = 0; i < batches.Count; i++)
        {
            var batchText = batches[i];
            if (string.IsNullOrWhiteSpace(batchText)) continue;

            foreach (var (pattern, hint) in hints)
            {
                if (pattern.IsMatch(batchText))
                {
                    issues.Add(new SqlScriptSyntaxIssue
                    {
                        Source = prefix,
                        BatchNumber = i + 1,
                        Line = 0,
                        Column = 0,
                        Message = $"⚠ {hint}"
                    });
                }
            }
        }
    }

    private static List<SqlScriptSyntaxIssue> DeduplicateIssues(List<SqlScriptSyntaxIssue> issues)
    {
        var seen = new HashSet<string>();
        var list = new List<SqlScriptSyntaxIssue>();
        foreach (var x in issues)
        {
            var key = $"{x.Source}|{x.BatchNumber}|{x.Line}|{x.Column}|{x.Message}";
            if (seen.Add(key))
                list.Add(x);
        }

        return list;
    }

    /// CREATE/ALTER PROCEDURE, VIEW, FUNCTION, TRIGGER gibi DDL'ler bir batch'te
    /// ilk statement olmak zorunda — SET NOEXEC ON öncesine koyulamaz; bu batches atlanır.
    private static readonly Regex DdlFirstStatementPattern = new(
        @"^\s*(CREATE|ALTER)\s+(OR\s+ALTER\s+)?(PROCEDURE|PROC|VIEW|FUNCTION|TRIGGER)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Her batch: <c>SET NOEXEC ON</c> + metin tek <see cref="SqlCommand"/> içinde (derleme hataları; geçersiz veri türü adları dahil).
    /// DDL batches (PROCEDURE/VIEW/FUNCTION/TRIGGER) ScriptDom tarafından zaten kontrol edildiğinden atlanır.
    /// </summary>
    private SqlScriptSyntaxResult? TryValidateWithSqlServerNoExec(IReadOnlyList<string> batches, string prefix)
    {
        try
        {
            using var conn = new SqlConnection(_parseOnlyConnectionString);
            conn.Open();

            var issues = new List<SqlScriptSyntaxIssue>();
            try
            {
                for (var i = 0; i < batches.Count; i++)
                {
                    var batchText = batches[i];
                    if (string.IsNullOrWhiteSpace(batchText))
                        continue;

                    // CREATE/ALTER PROCEDURE, VIEW, FUNCTION, TRIGGER → NOEXEC wrap yasak
                    if (DdlFirstStatementPattern.IsMatch(batchText))
                        continue;

                    var batchNumber = i + 1;
                    var combined = "SET NOEXEC ON;\r\n" + batchText;
                    try
                    {
                        using var cmd = new SqlCommand(combined, conn) { CommandTimeout = 120 };
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        issues.Add(MakeIssue(prefix, batchNumber, ex));
                    }
                }
            }
            finally
            {
                try
                {
                    using var off = new SqlCommand("SET NOEXEC OFF;", conn);
                    off.ExecuteNonQuery();
                }
                catch
                {
                    /* ignore */
                }
            }

            return new SqlScriptSyntaxResult { IsValid = issues.Count == 0, Issues = issues };
        }
        catch
        {
            return null;
        }
    }

    private static SqlScriptSyntaxIssue MakeIssue(string prefix, int batchNumber, SqlException ex)
    {
        return new SqlScriptSyntaxIssue
        {
            Source = prefix,
            BatchNumber = batchNumber,
            Line = FirstLine(ex),
            Column = 0,
            Message = ex.Message
        };
    }

    private static int FirstLine(SqlException ex)
    {
        if (ex.Errors.Count <= 0)
            return 0;
        return ex.Errors[0].LineNumber;
    }

    private static SqlScriptSyntaxResult ValidateWithScriptDom(IReadOnlyList<string> batches, string prefix)
    {
        var issues = new List<SqlScriptSyntaxIssue>();

        for (var i = 0; i < batches.Count; i++)
        {
            var batchNumber = i + 1;
            var batchText = batches[i];
            if (string.IsNullOrWhiteSpace(batchText))
                continue;

            IList<ParseError> parseErrors;
            TSqlFragment fragment;
            using (var reader = new StringReader(batchText))
                fragment = Parser.Parse(reader, out parseErrors);

            if (parseErrors != null)
            {
                foreach (var e in parseErrors)
                {
                    issues.Add(new SqlScriptSyntaxIssue
                    {
                        Source = prefix,
                        BatchNumber = batchNumber,
                        Line = e.Line,
                        Column = e.Column,
                        Message = e.Message ?? "Hata"
                    });
                }
            }

            if (parseErrors == null || parseErrors.Count == 0)
            {
                if (fragment is TSqlScript ts)
                {
                    foreach (var batch in ts.Batches)
                    {
                        if (batch.Statements == null || batch.Statements.Count == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(batchText))
                            {
                                issues.Add(new SqlScriptSyntaxIssue
                                {
                                    Source = prefix,
                                    BatchNumber = batchNumber,
                                    Line = 1,
                                    Column = 1,
                                    Message = "Geçerli T-SQL komutu yok (en az bir ifade gerekir)."
                                });
                            }
                        }
                    }
                }
            }
        }

        return new SqlScriptSyntaxResult { IsValid = issues.Count == 0, Issues = issues };
    }

    /// <summary>SSMS'teki gibi; satırı yalnızca GO olan yerlerden böler.</summary>
    internal static IEnumerable<string> SplitGoBatches(string sql)
    {
        var normalized = sql.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var current = new StringBuilder();

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line.Trim(), "^GO$", RegexOptions.IgnoreCase))
            {
                var part = current.ToString();
                current.Clear();
                if (!string.IsNullOrWhiteSpace(part))
                    yield return part.Trim();
            }
            else
            {
                if (current.Length > 0)
                    current.Append('\n');
                current.Append(line);
            }
        }

        var last = current.ToString();
        if (!string.IsNullOrWhiteSpace(last))
            yield return last.Trim();
    }

    public static string FormatIssueList(IReadOnlyList<SqlScriptSyntaxIssue> issues)
    {
        if (issues == null || issues.Count == 0)
            return "";
        var sb = new StringBuilder();
        foreach (var i in issues)
            sb.AppendLine($"{i.Source} batch {i.BatchNumber}, satır {i.Line}, sütun {i.Column}: {i.Message}");
        return sb.ToString().TrimEnd();
    }
}
