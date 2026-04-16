using System.Text.RegularExpressions;

namespace BLL.Services;

/// <summary>
/// SQL metninden olası tablo/nesne adlarını kaba regex ile çıkarır (uyarı amaçlı; tam SQL parser değildir).
/// </summary>
public static class SqlReferencedTableExtractor
{
    // DML — veri değiştiren ifadeler
    private static readonly Regex[] DmlPatterns =
    {
        new(@"(?i)\bINSERT\s+INTO\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bUPDATE\s+(?<t>[\w\.\[\]\""`]+)\b", RegexOptions.Compiled),
        new(@"(?i)\bDELETE\s+FROM\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bTRUNCATE\s+TABLE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bMERGE\s+(?<t>[\w\.\[\]\""`]+)\b", RegexOptions.Compiled),
        new(@"(?i)\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bDROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
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
        "SET", "WITH", "TOP", "JOIN", "FROM", "INTO", "WHERE", "BY", "AS"
    };

    public static HashSet<string> ExtractTables(string? sql)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;

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

    public static HashSet<string> ExtractRecordIds(string? sql)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;

        foreach (Match m in Regex.Matches(sql, @"(?i)\bID\s*=\s*(?<id>\d+)\b"))
        {
            if (!m.Groups["id"].Success) continue;
            set.Add($"ID:{m.Groups["id"].Value}");
        }

        foreach (Match m in Regex.Matches(sql, @"(?i)\bID\s+IN\s*\((?<ids>[0-9,\s]+)\)"))
        {
            if (!m.Groups["ids"].Success) continue;
            foreach (var raw in m.Groups["ids"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(raw, out var parsed))
                    set.Add($"ID:{parsed}");
            }
        }

        return set;
    }

    /// <summary>
    /// Köşeli parantez ve tırnak işaretlerini temizler; şema önekini korur.
    /// Örn: [dbo].[Users] → dbo.Users | Users → Users
    /// </summary>
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
