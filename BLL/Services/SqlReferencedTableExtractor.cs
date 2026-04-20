using System.Text;
using System.Text.RegularExpressions;

namespace BLL.Services;

public static class SqlReferencedTableExtractor
{
    // DML — veri değiştiren ifadeler
    private static readonly Regex[] DmlPatterns =
    {
        new(@"(?i)\bINSERT\s+INTO\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        // UPDATE TOP (n) dbo.Table — aksi halde "TOP" tablo sanılıyordu
        new(@"(?i)\bUPDATE\s+(?:TOP\s*\([^)]*\)\s+)?(?<t>[\w\.\[\]\""`]+)\b", RegexOptions.Compiled),
        new(@"(?i)\bDELETE\s+FROM\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bTRUNCATE\s+TABLE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        // MERGE [INTO] hedef — INTO anahtar kelimesi tablo sayılmasın
        new(@"(?i)\bMERGE\s+(?:INTO\s+)?(?<t>[\w\.\[\]\""`]+)\b", RegexOptions.Compiled),
        new(@"(?i)\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bDROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        // FOREIGN KEY ... REFERENCES şema.tablo
        new(@"(?i)\bREFERENCES\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
    };

    // DDL — nesne oluşturma/değiştirme
    private static readonly Regex[] DdlPatterns =
    {
        new(@"(?i)\b(?:CREATE|ALTER)\s+(?:OR\s+ALTER\s+)?PROCEDURE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\b(?:CREATE|ALTER)\s+(?:OR\s+ALTER\s+)?FUNCTION\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\b(?:CREATE|ALTER)\s+(?:OR\s+ALTER\s+)?VIEW\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\b(?:CREATE|ALTER)\s+(?:UNIQUE\s+)?INDEX\s+[\w\.\[\]\""`]+\s+ON\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
    };

    private static readonly HashSet<string> SqlNoise = new(StringComparer.OrdinalIgnoreCase)
    {
        "SET", "WITH", "TOP", "JOIN", "FROM", "INTO", "WHERE", "BY", "AS",
        // Açıklama / araç metinlerinde geçen "UPDATE command(s)" vb. yanlış yakalanmasın
        "COMMAND", "COMMANDS",
        // PRINT N'Update complete.' gibi metinlerdeki "UPDATE complete" yanlış eşleşmesi
        "COMPLETE",
        // İstatistik / yardımcı ifadeler
        "STATISTICS"
    };

    private static readonly Regex BlockCommentRx = new(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);
    private static readonly Regex LineCommentRx = new(@"--[^\r\n]*", RegexOptions.Compiled);

    private static string StripSqlComments(string sql)
    {
        var s = BlockCommentRx.Replace(sql, " ");
        return LineCommentRx.Replace(s, " ");
    }

    private static string StripSqlStringLiterals(string sql)
    {
        var sb = new StringBuilder(sql.Length);
        for (var i = 0; i < sql.Length;)
        {
            if (i < sql.Length - 1 && (sql[i] == 'N' || sql[i] == 'n') && sql[i + 1] == '\'')
            {
                i += 2;
                i = SkipSingleQuotedStringRun(sql, i);
                sb.Append(' ');
                continue;
            }

            if (sql[i] == '\'')
            {
                i++;
                i = SkipSingleQuotedStringRun(sql, i);
                sb.Append(' ');
                continue;
            }

            sb.Append(sql[i]);
            i++;
        }

        return sb.ToString();
    }

    private static int SkipSingleQuotedStringRun(string sql, int i)
    {
        while (i < sql.Length)
        {
            if (sql[i] == '\'')
            {
                if (i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            i++;
        }

        return i;
    }

    private static string PrepareSqlForObjectScan(string sql)
    {
        sql = StripSqlComments(sql);
        sql = StripSqlStringLiterals(sql);
        return sql;
    }

    public static HashSet<string> ExtractTables(string? sql)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;

        sql = PrepareSqlForObjectScan(sql);

        foreach (var rx in DmlPatterns)
            AddMatches(rx, sql, set);

        foreach (var rx in DdlPatterns)
            AddMatches(rx, sql, set);

        return set;
    }

    private static void AddMatches(Regex rx, string sql, HashSet<string> set)
    {
        foreach (Match m in rx.Matches(sql))
        {
            if (!m.Groups["t"].Success) continue;
            var name = NormalizeObjectName(m.Groups["t"].Value);
            if (name.Length == 0 || SqlNoise.Contains(name)) continue;
            set.Add(name);
        }
    }

    // "UserId = 42" / "ID = 42" / "OrderId = 42" gibi herhangi bir *Id/*ID kolonu
    private static readonly Regex RecordEqPattern =
        new(@"(?i)\b(?<col>\w*[Ii][Dd])\s*=\s*(?<id>\d+)\b", RegexOptions.Compiled);

    // "UserId IN (1,2,3)" gibi herhangi bir *Id/*ID kolonu
    private static readonly Regex RecordInPattern =
        new(@"(?i)\b(?<col>\w*[Ii][Dd])\s+IN\s*\((?<ids>[0-9,\s]+)\)", RegexOptions.Compiled);

    // Regex eşleşmesi olmasına rağmen gerçek ID kolonu olmayan gürültülü kelimeler
    private static readonly HashSet<string> RecordNoise = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACID", "AVOID", "FORBID", "INVALID", "VALID", "REBUILD", "PERIOD"
    };

    public static HashSet<string> ExtractRecordIds(string? sql)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;

        sql = PrepareSqlForObjectScan(sql);

        foreach (Match m in RecordEqPattern.Matches(sql))
        {
            if (!m.Groups["col"].Success || !m.Groups["id"].Success) continue;
            var col = m.Groups["col"].Value;
            if (RecordNoise.Contains(col)) continue;
            set.Add($"{col.ToUpperInvariant()}:{m.Groups["id"].Value}");
        }

        foreach (Match m in RecordInPattern.Matches(sql))
        {
            if (!m.Groups["col"].Success || !m.Groups["ids"].Success) continue;
            var col = m.Groups["col"].Value;
            if (RecordNoise.Contains(col)) continue;
            var colUpper = col.ToUpperInvariant();
            foreach (var raw in m.Groups["ids"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (long.TryParse(raw, out var parsed))
                    set.Add($"{colUpper}:{parsed}");
            }
        }

        return set;
    }

    private static string NormalizeObjectName(string raw)
    {
        var s = raw.Trim();

        // Parçaları noktaya göre böl, her birini temizle ve tekrar birleştir (şema korunur)
        var parts = s.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cleaned = parts
            .Select(p => p.Trim('[', ']', '"', '`'))
            .Where(p => p.Length > 0)
            .ToArray();

        return cleaned.Length > 0 ? string.Join(".", cleaned) : string.Empty;
    }
}
