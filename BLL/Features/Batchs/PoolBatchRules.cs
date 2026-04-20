using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Batchs
{
    public static class PoolBatchRules
    {
        public static async Task<(bool Ok, string? Error)> ValidateSubtreeReadyForReleaseAsync(
            MyContext db,
            long rootBatchId,
            CancellationToken cancellationToken = default)
        {
            var allFlat = await db.Batches.AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Select(b => new { b.Id, b.ParentBatchId })
                .ToListAsync(cancellationToken);

            var inSubtree = new HashSet<long> { rootBatchId };
            var queue = new Queue<long>();
            queue.Enqueue(rootBatchId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var c in allFlat.Where(x => x.ParentBatchId == id))
                {
                    if (inSubtree.Add(c.Id))
                        queue.Enqueue(c.Id);
                }
            }

            var subIds = inSubtree.ToList();
            var treeScripts = await db.Scripts.AsNoTracking()
                .Where(s => s.BatchId != null && subIds.Contains(s.BatchId.Value) && !s.IsDeleted && s.Status != ScriptStatus.Deleted)
                .Select(s => new { s.Id, s.Status })
                .ToListAsync(cancellationToken);

            if (treeScripts.Count == 0)
                return (false, $"\"{await BatchNameAsync(db, rootBatchId, cancellationToken)}\" altında en az bir script olmalı.");

            var idSet = treeScripts.Select(s => s.Id).ToHashSet();
            var openConflict = await db.Conflicts.AsNoTracking()
                .AnyAsync(c =>
                        !c.IsDeleted &&
                        c.ResolvedAt == null &&
                        (idSet.Contains(c.ScriptId) || idSet.Contains(c.ConflictingScriptId)),
                    cancellationToken);
            if (openConflict)
                return (false, $"\"{await BatchNameAsync(db, rootBatchId, cancellationToken)}\" altında çözülmemiş çakışma var.");

            if (treeScripts.Any(s => s.Status != ScriptStatus.Ready))
                return (false, $"\"{await BatchNameAsync(db, rootBatchId, cancellationToken)}\" altında aktif scriptlerin tamamı Hazır olmalı.");

            return (true, null);
        }

        private static async Task<string> BatchNameAsync(MyContext db, long id, CancellationToken ct)
        {
            var n = await db.Batches.AsNoTracking().Where(b => b.Id == id).Select(b => b.Name)
                .FirstOrDefaultAsync(ct);
            return n ?? "Batch";
        }

        public static List<long> CollectDescendingIds(IReadOnlyList<(long Id, long? ParentBatchId)> allFlat, long rootBatchId)
        {
            var result = new List<long>();
            var q = new Queue<long>();
            foreach (var c in allFlat.Where(x => x.ParentBatchId == rootBatchId))
                q.Enqueue(c.Id);
            while (q.Count > 0)
            {
                var id = q.Dequeue();
                result.Add(id);
                foreach (var c in allFlat.Where(x => x.ParentBatchId == id))
                    q.Enqueue(c.Id);
            }

            return result;
        }
    }
}
