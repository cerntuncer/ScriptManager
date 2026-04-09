using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Batchs
{
    public static class PoolBatchRules
    {
        /// <summary>Havuz kökü alt ağacında en az bir script olmalı ve aktif scriptlerin tamamı Hazır olmalı.</summary>
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
            var statuses = await db.Scripts.AsNoTracking()
                .Where(s => s.BatchId != null && subIds.Contains(s.BatchId.Value) && !s.IsDeleted && s.Status != ScriptStatus.Deleted)
                .Select(s => s.Status)
                .ToListAsync(cancellationToken);

            if (statuses.Count == 0)
                return (false, $"\"{await BatchNameAsync(db, rootBatchId, cancellationToken)}\" altında en az bir script olmalı.");

            if (statuses.Any(s => s != ScriptStatus.Ready))
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
