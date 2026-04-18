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

    /// <summary>
    /// SQL metninden kayıt bazlı ID değerlerini çıkarır.
    /// Desteklenen kalıplar:
    ///   ID = 42             → "ID:42"
    ///   UserId = 42         → "USERID:42"
    ///   OrderId IN (1,2,3)  → "ORDERID:1", "ORDERID:2", "ORDERID:3"
    /// </summary>
    public static HashSet<string> ExtractRecordIds(string? sql)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;

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
