using DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ScriptManager.Data;

public static class BatchTreeHelper
{
    public static async Task<bool> IsOrphanRootAsync(MyContext db, long batchId)
    {
        return await db.Batches.AsNoTracking()
            .AnyAsync(b => b.Id == batchId && !b.IsDeleted && b.ReleaseId == null && b.ParentBatchId == null);
    }

    public static async Task<bool> EntireSubtreeIsOrphanAsync(MyContext db, long rootBatchId)
    {
        var ids = await CollectSubtreeIdsAsync(db, rootBatchId);
        if (ids.Count == 0) return false;
        return !await db.Batches.AsNoTracking()
            .AnyAsync(b => ids.Contains(b.Id) && !b.IsDeleted && b.ReleaseId != null);
    }

    public static async Task<List<long>> CollectSubtreeIdsAsync(MyContext db, long rootBatchId)
    {
        var result = new List<long>();
        var queue = new Queue<long>();
        queue.Enqueue(rootBatchId);
        var seen = new HashSet<long>();

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!seen.Add(id)) continue;
            result.Add(id);

            var children = await db.Batches.AsNoTracking()
                .Where(b => !b.IsDeleted && b.ParentBatchId == id)
                .Select(b => b.Id)
                .ToListAsync();
            foreach (var child in children)
                queue.Enqueue(child);
        }

        return result;
    }

    public static async Task PropagateReleaseIdAsync(MyContext db, long rootBatchId, long releaseId)
    {
        var ids = await CollectSubtreeIdsAsync(db, rootBatchId);
        if (ids.Count == 0) return;
        var nodes = await db.Batches.Where(b => ids.Contains(b.Id) && !b.IsDeleted).ToListAsync();
        foreach (var node in nodes)
            node.ReleaseId = releaseId;
        await db.SaveChangesAsync();
    }

    public static async Task<bool> SiblingNameExistsAsync(MyContext db, long? parentBatchId, long? releaseId, string name)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (normalized.Length == 0) return false;
        return await db.Batches.AsNoTracking().AnyAsync(b =>
            !b.IsDeleted &&
            b.ParentBatchId == parentBatchId &&
            b.ReleaseId == releaseId &&
            b.Name == normalized);
    }
}
