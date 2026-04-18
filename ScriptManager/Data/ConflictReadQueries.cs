using DAL.Context;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.Conflict;

namespace ScriptManager.Data;

public static class ConflictReadQueries
{
    public static async Task<List<ConflictRowViewModel>> ListUnresolvedAsync(MyContext db)
    {
        var rows = await db.Conflicts.AsNoTracking()
            .Include(c => c.Script).ThenInclude(s => s.Developer)
            .Include(c => c.ConflictingScript).ThenInclude(s => s.Developer)
            .Where(c => c.ResolvedAt == null && !c.IsDeleted)
            .OrderByDescending(c => c.DetectedAt)
            .ToListAsync();

        return rows.Select(ToViewModel).ToList();
    }

    public static async Task<List<ConflictRowViewModel>> ListRecentlyResolvedAsync(MyContext db, int take = 15)
    {
        var rows = await db.Conflicts.AsNoTracking()
            .Include(c => c.Script).ThenInclude(s => s.Developer)
            .Include(c => c.ConflictingScript).ThenInclude(s => s.Developer)
            .Include(c => c.ResolvedByUser)
            .Where(c => c.ResolvedAt != null && !c.IsDeleted)
            .OrderByDescending(c => c.ResolvedAt)
            .Take(take)
            .ToListAsync();

        return rows.Select(ToViewModel).ToList();
    }

    private static ConflictRowViewModel ToViewModel(Conflict c) => new()
    {
        ConflictId      = c.Id,
        TableName       = c.TableName,
        DetectedAt      = c.DetectedAt,
        ResolvedAt      = c.ResolvedAt,
        ResolvedByName  = c.ResolvedByUser?.Name,
        ScriptId        = c.ScriptId,
        ScriptName      = c.Script?.Name ?? $"#{c.ScriptId}",
        ScriptDeveloper = c.Script?.Developer?.Name ?? "—",
        OtherScriptId   = c.ConflictingScriptId,
        OtherScriptName = c.ConflictingScript?.Name ?? $"#{c.ConflictingScriptId}",
        OtherDeveloper  = c.ConflictingScript?.Developer?.Name ?? "—"
    };
}
