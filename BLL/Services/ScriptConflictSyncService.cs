using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class ScriptConflictSyncService : IScriptConflictSyncService
{
    private readonly MyContext _db;

    public ScriptConflictSyncService(MyContext db)
    {
        _db = db;
    }

    public async Task SyncAfterScriptSavedAsync(long scriptId, CancellationToken cancellationToken = default)
    {
        var script = await _db.Scripts
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.Id == scriptId && !s.IsDeleted, cancellationToken);
        if (script == null || script.Status == ScriptStatus.Deleted) return;

        var myKeys = SqlConflictKeyExtractor.ExtractFromScript(script.SqlScript, script.RollbackScript);
        var peers  = await GetPeerScriptsAsync(script, cancellationToken);

        var desired = new HashSet<(long Min, long Max, string Key)>();
        foreach (var peer in peers)
        {
            if (peer.Id == script.Id) continue;
            var peerKeys = SqlConflictKeyExtractor.ExtractFromScript(peer.SqlScript, peer.RollbackScript);
            var min = Math.Min(script.Id, peer.Id);
            var max = Math.Max(script.Id, peer.Id);

            // Tüm key çiftlerini kural matrisi ile karşılaştır, çakışanları topla
            var topics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mk in myKeys)
            foreach (var pk in peerKeys)
            {
                if (!ConflictKey.DoConflict(mk, pk)) continue;
                topics.Add(ConflictKey.CanonicalKey(mk, pk));
            }

            foreach (var topic in topics)
                desired.Add((min, max, topic));
        }

        var existingRows = await _db.Conflicts
            .Where(c => c.ResolvedAt == null &&
                        (c.ScriptId == script.Id || c.ConflictingScriptId == script.Id))
            .ToListAsync(cancellationToken);

        var affectedScriptIds = new HashSet<long> { script.Id };
        foreach (var peer in peers)
            affectedScriptIds.Add(peer.Id);

        var existingKeys = new HashSet<(long Min, long Max, string Key)>();
        foreach (var c in existingRows)
        {
            var min = Math.Min(c.ScriptId, c.ConflictingScriptId);
            var max = Math.Max(c.ScriptId, c.ConflictingScriptId);
            var key = (min, max, c.TableName);
            if (!desired.Contains(key))
            {
                _db.Conflicts.Remove(c);
                affectedScriptIds.Add(c.ScriptId);
                affectedScriptIds.Add(c.ConflictingScriptId);
            }
            else
                existingKeys.Add(key);
        }

        foreach (var d in desired)
        {
            if (existingKeys.Contains(d)) continue;
            var row = new Conflict
            {
                ScriptId = d.Min,
                ConflictingScriptId = d.Max,
                TableName = d.Key,
                DetectedAt = DateTime.UtcNow
            };
            await _db.Conflicts.AddAsync(row, cancellationToken);
            affectedScriptIds.Add(d.Min);
            affectedScriptIds.Add(d.Max);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var sid in affectedScriptIds)
            await ApplyConflictStatusForScriptAsync(sid, cancellationToken);
    }

    public Task<bool> HasUnresolvedConflictsAsync(long scriptId, CancellationToken cancellationToken = default)
    {
        return _db.Conflicts.AsNoTracking()
            .AnyAsync(c => c.ResolvedAt == null &&
                           (c.ScriptId == scriptId || c.ConflictingScriptId == scriptId),
                cancellationToken);
    }

    public Task RecomputeScriptStatusAsync(long scriptId, CancellationToken cancellationToken = default) =>
        ApplyConflictStatusForScriptAsync(scriptId, cancellationToken);

    public async Task RecomputeScriptsAfterConflictChangeAsync(long scriptId, long otherScriptId, CancellationToken cancellationToken = default)
    {
        await ApplyConflictStatusForScriptAsync(scriptId, cancellationToken);
        if (otherScriptId != scriptId)
            await ApplyConflictStatusForScriptAsync(otherScriptId, cancellationToken);
    }

    private async Task<List<Script>> GetPeerScriptsAsync(Script script, CancellationToken cancellationToken)
    {
        IQueryable<Script> q = _db.Scripts
            .AsNoTracking()
            .Include(s => s.Batch)
            .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted && s.Id != script.Id);

        if (!script.BatchId.HasValue)
        {
            q = q.Where(s => s.BatchId == null);
            return await q.ToListAsync(cancellationToken);
        }

        var releaseId = await ResolveReleaseIdAsync(script.BatchId.Value, cancellationToken);

        if (releaseId.HasValue)
        {
            q = q.Where(s =>
                s.Batch != null && !s.Batch.IsDeleted && s.Batch.ReleaseId == releaseId);
        }
        else
        {
            var rootId = await GetRootBatchIdAsync(script.BatchId.Value, cancellationToken);
            var batchIds = await GetSubtreeBatchIdsAsync(rootId, cancellationToken);
            q = q.Where(s => s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value));
        }

        return await q.ToListAsync(cancellationToken);
    }

    private async Task<long?> ResolveReleaseIdAsync(long batchId, CancellationToken cancellationToken)
    {
        long? current = batchId;
        while (current.HasValue)
        {
            var b = await _db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == current.Value && !x.IsDeleted, cancellationToken);
            if (b == null) return null;
            if (b.ReleaseId.HasValue) return b.ReleaseId;
            current = b.ParentBatchId;
        }

        return null;
    }

    private async Task<long> GetRootBatchIdAsync(long batchId, CancellationToken cancellationToken)
    {
        long? current = batchId;
        long root = batchId;
        while (current.HasValue)
        {
            var b = await _db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == current.Value && !x.IsDeleted, cancellationToken);
            if (b == null) break;
            root = b.Id;
            current = b.ParentBatchId;
        }

        return root;
    }

    private async Task<List<long>> GetSubtreeBatchIdsAsync(long rootBatchId, CancellationToken cancellationToken)
    {
        var result = new List<long>();
        var queue = new Queue<long>();
        queue.Enqueue(rootBatchId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(id);
            var children = await _db.Batches.AsNoTracking()
                .Where(x => x.ParentBatchId == id && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            foreach (var c in children)
                queue.Enqueue(c);
        }

        return result;
    }

    private async Task ApplyConflictStatusForScriptAsync(long scriptId, CancellationToken cancellationToken)
    {
        var script = await _db.Scripts.FirstOrDefaultAsync(s => s.Id == scriptId && !s.IsDeleted, cancellationToken);
        if (script == null || script.Status == ScriptStatus.Deleted) return;

        var open = await _db.Conflicts.AsNoTracking()
            .AnyAsync(c => c.ResolvedAt == null &&
                           (c.ScriptId == scriptId || c.ConflictingScriptId == scriptId),
                cancellationToken);

        if (open)
        {
            if (script.Status != ScriptStatus.Conflict)
            {
                script.StatusBeforeConflict = script.Status;
                script.Status = ScriptStatus.Conflict;
                _db.Scripts.Update(script);
            }
        }
        else
        {
            if (script.Status == ScriptStatus.Conflict)
            {
                script.Status = script.StatusBeforeConflict ?? ScriptStatus.Draft;
                script.StatusBeforeConflict = null;
                _db.Scripts.Update(script);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
