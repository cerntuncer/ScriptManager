using System.Text.RegularExpressions;

namespace BLL.Services;

/// <summary>
/// SQL metninden olası tablo adlarını kaba regex ile çıkarır (uyarı amaçlı; tam SQL parser değildir).
/// </summary>
public static class SqlReferencedTableExtractor
{
    private static readonly Regex[] Patterns =
    {
        new(@"(?i)\bINSERT\s+INTO\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bUPDATE\s+(?<t>[\w\.\[\]\""`]+)\b", RegexOptions.Compiled),
        new(@"(?i)\bDELETE\s+FROM\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bTRUNCATE\s+TABLE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bALTER\s+TABLE\s+(?<t>[\w\.\[\]\""`]+)", RegexOptions.Compiled),
        new(@"(?i)\bMERGE\s+(?<t>[\w\.\[\]\""`]+)\b", RegexOptions.Compiled),
    };

    private static readonly HashSet<string> SqlNoise = new(StringComparer.OrdinalIgnoreCase)
    {
        "SET", "WITH", "TOP", "JOIN", "FROM", "INTO", "WHERE", "BY", "AS"
    };

    public static HashSet<string> ExtractTables(string? sql)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;

        foreach (var rx in Patterns)
        {
            foreach (Match m in rx.Matches(sql))
            {
                if (!m.Groups["t"].Success) continue;
                var name = NormalizeTableName(m.Groups["t"].Value);
                if (name.Length == 0 || SqlNoise.Contains(name)) continue;
                set.Add(name);
            }
        }

        return set;
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

    private static string NormalizeTableName(string raw)
    {
        var s = raw.Trim();
        s = s.Trim('[', ']', '"', '`');
        var parts = s.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var last = parts.Length > 0 ? parts[^1] : s;
        last = last.Trim('[', ']', '"', '`');
        return last;
    }
}
