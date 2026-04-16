using BLL.Features.Batchs;
using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.Batch;

namespace ScriptManager.Data;

public static class PoolBatchQueries
{
    /// <summary>
    /// Tüm havuz kökleri + aktif sürümlerin kök paketleri (aynı ağaçta). İptal sürümlerin batch'leri havuzda ReleaseId null olduğundan burada görünmez.
    /// </summary>
    public static async Task<List<PoolBatchTreeNodeDto>> GetPoolBatchTreeAsync(MyContext db,
        CancellationToken cancellationToken = default)
    {
        var activeReleaseIds = await db.Releases.AsNoTracking()
            .Where(r => !r.IsDeleted && !r.IsCancelled)
            .Select(r => r.Id)
            .ToHashSetAsync(cancellationToken);

        var verByRel = await db.Releases.AsNoTracking()
            .Where(r => !r.IsDeleted && !r.IsCancelled)
            .ToDictionaryAsync(r => r.Id, r => r.Version, cancellationToken);

        var rows = await db.Batches.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new { b.Id, b.Name, b.ParentBatchId, b.IsLocked, b.ReleaseId })
            .ToListAsync(cancellationToken);

        var idSet = rows.Select(r => r.Id).ToHashSet();
        var hasChildSet = rows
            .Where(r => r.ParentBatchId.HasValue && idSet.Contains(r.ParentBatchId.Value))
            .Select(r => r.ParentBatchId!.Value)
            .Distinct()
            .ToHashSet();

        var scriptBatchIds = await db.Scripts.AsNoTracking()
            .Where(s =>
                s.BatchId != null && !s.IsDeleted && s.Status != ScriptStatus.Deleted)
            .Select(s => s.BatchId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var scriptBatchSet = scriptBatchIds.ToHashSet();

        async Task<PoolBatchTreeNodeDto> BuildNode(long nodeId)
        {
            var row = rows.First(r => r.Id == nodeId);
            var children = rows.Where(r => r.ParentBatchId == nodeId).OrderBy(r => r.Name).ThenBy(r => r.Id)
                .ToList();

            var inLockedRelease = row.ReleaseId.HasValue && activeReleaseIds.Contains(row.ReleaseId.Value);
            string? relVer = null;
            if (row.ReleaseId.HasValue && verByRel.TryGetValue(row.ReleaseId.Value, out var rv))
                relVer = rv;

            var (pkgOk, _) = row.ReleaseId.HasValue
                ? (false, (string?)null)
                : await PoolBatchRules.ValidateSubtreeReadyForReleaseAsync(db, nodeId, cancellationToken);

            var childDtos = new List<PoolBatchTreeNodeDto>();
            foreach (var c in children)
                childDtos.Add(await BuildNode(c.Id));

            var isRoot = !row.ParentBatchId.HasValue;
            return new PoolBatchTreeNodeDto
            {
                BatchId = nodeId,
                Name = row.Name,
                IsLocked = row.IsLocked,
                CanAddScript = !row.IsLocked && !hasChildSet.Contains(nodeId),
                CanAddChild = !row.IsLocked && !scriptBatchSet.Contains(nodeId),
                CanPackageRelease = pkgOk,
                CanDelete = isRoot && !row.IsLocked && !row.ReleaseId.HasValue,
                LinkedReleaseId = inLockedRelease ? row.ReleaseId : null,
                LinkedReleaseVersion = inLockedRelease ? relVer : null,
                Children = childDtos
            };
        }

        var result = new List<PoolBatchTreeNodeDto>();
        foreach (var root in rows.Where(r => r.ParentBatchId == null).OrderBy(r => r.Name).ThenBy(r => r.Id))
            result.Add(await BuildNode(root.Id));

        return result;
    }
}
