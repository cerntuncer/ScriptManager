using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Services;

/// <summary>
/// SQL metninde plan/önbellek invalidasyonu tetikleyebilecek kalıpların kaba taraması (parser değildir).
/// </summary>
public static class SqlCacheBustAnalyzer
{
    private static readonly string[] Keywords =
    {
        "SP_RECOMPILE",
        "FREEPROCCACHE",
        "DROPCLEANBUFFERS",
        "DROPCLEANBUFFERS ",
        "UPDATE STATISTICS",
        "DBCC FREEPROCCACHE",
        "DBCC DROPCLEANBUFFERS",
        "DBCC FLUSHPROCINDB",
        "CLEAR PROCEDURE CACHE",
        "ALTER DATABASE",
        "SET COMPATIBILITY_LEVEL",
        "DBCC FREESYSTEMCACHE",
        "RECONFIGURE",
    };

    public static IReadOnlyList<string> FindMatches(string? sqlScript, string? rollbackScript)
    {
        var combined = $"{sqlScript}\n{rollbackScript}";
        if (string.IsNullOrWhiteSpace(combined))
            return Array.Empty<string>();

        var upper = combined.ToUpperInvariant();
        var hit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in Keywords)
        {
            if (upper.Contains(k, StringComparison.Ordinal))
                hit.Add(k.Trim());
        }

        return hit.OrderBy(x => x).ToList();
    }

    public static bool HasAnyMatch(string? sqlScript, string? rollbackScript) =>
        FindMatches(sqlScript, rollbackScript).Count > 0;

    public static string? SummaryLabel(string? sqlScript, string? rollbackScript)
    {
        var m = FindMatches(sqlScript, rollbackScript);
        return m.Count == 0 ? null : string.Join(", ", m);
    }
}
