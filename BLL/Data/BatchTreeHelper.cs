using DAL.Context;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Data;

public static class BatchTreeHelper
{
    public static async Task<List<long>> CollectSubtreeIdsAsync(MyContext db, long rootBatchId)
    {
        var result = new List<long>();
        var queue = new Queue<long>();
        queue.Enqueue(rootBatchId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(id);
            var children = await db.Batches.AsNoTracking()
                .Where(b => b.ParentBatchId == id && !b.IsDeleted)
                .Select(b => b.Id)
                .ToListAsync();
            foreach (var c in children)
                queue.Enqueue(c);
        }

        return result;
    }

    public static async Task PropagateReleaseIdAsync(MyContext db, long rootBatchId, long releaseId)
    {
        var ids = await CollectSubtreeIdsAsync(db, rootBatchId);
        var batches = await db.Batches.Where(b => ids.Contains(b.Id)).ToListAsync();
        foreach (var b in batches)
            b.ReleaseId = releaseId;
    }

    /// <summary>Kök batch: üstü yok ve henüz bir release'e bağlı değil.</summary>
    public static async Task<bool> IsOrphanRootAsync(MyContext db, long batchId)
    {
        var b = await db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId && !x.IsDeleted);
        return b != null && b.ReleaseId == null && b.ParentBatchId == null;
    }

    public static async Task<bool> EntireSubtreeIsOrphanAsync(MyContext db, long rootBatchId)
    {
        var ids = await CollectSubtreeIdsAsync(db, rootBatchId);
        var count = await db.Batches.AsNoTracking()
            .CountAsync(b => ids.Contains(b.Id) && !b.IsDeleted && b.ReleaseId != null);
        return count == 0;
    }

    public static async Task<bool> SiblingNameExistsAsync(
        MyContext db,
        long? parentBatchId,
        long? releaseId,
        string name,
        long? excludeBatchId = null)
    {
        var q = db.Batches.Where(b => !b.IsDeleted && b.Name == name);
        if (parentBatchId.HasValue)
            q = q.Where(b => b.ParentBatchId == parentBatchId);
        else
            q = q.Where(b => b.ParentBatchId == null);

        if (releaseId.HasValue)
            q = q.Where(b => b.ReleaseId == releaseId);
        else
            q = q.Where(b => b.ReleaseId == null);

        if (excludeBatchId.HasValue)
            q = q.Where(b => b.Id != excludeBatchId.Value);

        return await q.AnyAsync();
    }

    /// <summary>Release kökünde (üst batch yok) benzersiz klasör adı: <c>Paket</c>, <c>Paket 2</c>, …</summary>
    public static async Task<string> AllocateDefaultRootBatchNameAsync(MyContext db, long releaseId)
    {
        const string prefix = "Paket";
        if (!await SiblingNameExistsAsync(db, null, releaseId, prefix))
            return prefix;
        for (var i = 2; i < 1000; i++)
        {
            var n = $"{prefix} {i}";
            if (!await SiblingNameExistsAsync(db, null, releaseId, n))
                return n;
        }

        throw new InvalidOperationException("Benzersiz kök batch adı üretilemedi.");
    }
}
