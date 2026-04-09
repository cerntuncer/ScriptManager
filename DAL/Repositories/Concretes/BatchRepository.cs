using DAL.Context;
using DAL.Entities;
using DAL.Repositories.Base;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Concretes
{
    public class BatchRepository : BaseRepository<Batch>, IBatchRepository
    {
        public BatchRepository(MyContext context) : base(context)
        {
        }

        public async Task<List<Batch>> GetWithScriptsAsync()
        {
            return await _context.Batches
                .Include(x => x.Scripts)
                .ToListAsync();
        }

        public async Task<Batch?> GetWithScriptsByIdAsync(long id)
        {
            return await _context.Batches
                .Include(x => x.Scripts)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Batch?> GetActiveByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _context.Batches
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsInScopeByNameAsync(long? parentBatchId, long? releaseId, string name,
            CancellationToken cancellationToken)
        {
            return await _context.Batches.AnyAsync(b =>
                !b.IsDeleted &&
                b.ParentBatchId == parentBatchId &&
                b.ReleaseId == releaseId &&
                b.Name == name, cancellationToken);
        }

        public async Task<HashSet<long>> GetAllowedIdsForReleaseAsync(long releaseId, long? rootBatchId,
            CancellationToken cancellationToken)
        {
            if (!rootBatchId.HasValue)
            {
                var ids = await _context.Batches.AsNoTracking()
                    .Where(b => b.ReleaseId == releaseId && !b.IsDeleted)
                    .Select(b => b.Id)
                    .ToListAsync(cancellationToken);
                return ids.ToHashSet();
            }

            var result = new HashSet<long>();
            var queue = new Queue<long>();
            queue.Enqueue(rootBatchId.Value);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!result.Add(id))
                    continue;

                var children = await _context.Batches.AsNoTracking()
                    .Where(b => b.ParentBatchId == id && !b.IsDeleted)
                    .Select(b => b.Id)
                    .ToListAsync(cancellationToken);

                foreach (var child in children)
                    queue.Enqueue(child);
            }

            return result;
        }

    }
}