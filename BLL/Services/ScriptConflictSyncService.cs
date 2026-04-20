using System.Linq;
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

        await InvalidateDismissalsIfSqlChangedAsync(script.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var myKeys = SqlConflictKeyExtractor.ExtractFromScript(script.SqlScript, script.RollbackScript);
        var peers  = await GetPeerScriptsAsync(script, cancellationToken);

        var desired = new Dictionary<(long Min, long Max), PairAgg>();
        foreach (var peer in peers)
        {
            if (peer.Id == script.Id) continue;
            var peerKeys = SqlConflictKeyExtractor.ExtractFromScript(peer.SqlScript, peer.RollbackScript);
            var min = Math.Min(script.Id, peer.Id);
            var max = Math.Max(script.Id, peer.Id);
            var pair = (min, max);

            foreach (var mk in myKeys)
            foreach (var pk in peerKeys)
            {
                if (!ConflictKey.DoConflict(mk, pk)) continue;
                var topic = ConflictKey.CanonicalKey(mk, pk);
                var sev = ConflictKey.SeverityForPair();
                if (!desired.TryGetValue(pair, out var agg))
                {
                    agg = new PairAgg();
                    desired[pair] = agg;
                }

                agg.Topics.Add(topic);
                if ((int)sev < (int)agg.Severity)
                    agg.Severity = sev;
            }
        }

        foreach (var key in desired.Keys.ToList())
        {
            if (await PairIsActivelyDismissedAsync(key.Min, key.Max, cancellationToken))
                desired.Remove(key);
        }

        var existingRows = await _db.Conflicts
            .Where(c => c.ResolvedAt == null &&
                        (c.ScriptId == script.Id || c.ConflictingScriptId == script.Id))
            .ToListAsync(cancellationToken);

        var affectedScriptIds = new HashSet<long> { script.Id };
        foreach (var peer in peers)
            affectedScriptIds.Add(peer.Id);

        var byPair = existingRows
            .GroupBy(c => (
                Min: Math.Min(c.ScriptId, c.ConflictingScriptId),
                Max: Math.Max(c.ScriptId, c.ConflictingScriptId)))
            .ToList();

        var handledDesiredPairs = new HashSet<(long Min, long Max)>();

        foreach (var grp in byPair)
        {
            var pair = grp.Key;
            if (!desired.TryGetValue(pair, out var want))
            {
                foreach (var c in grp)
                {
                    _db.Conflicts.Remove(c);
                    affectedScriptIds.Add(c.ScriptId);
                    affectedScriptIds.Add(c.ConflictingScriptId);
                }

                continue;
            }

            var combined = ConflictKey.CombineTopics(want.Topics);
            var ordered = grp.OrderBy(c => c.Id).ToList();
            var keeper = ordered[0];

            foreach (var c in ordered.Skip(1))
            {
                _db.Conflicts.Remove(c);
                affectedScriptIds.Add(c.ScriptId);
                affectedScriptIds.Add(c.ConflictingScriptId);
            }

            if (keeper.ScriptId != pair.Min || keeper.ConflictingScriptId != pair.Max ||
                keeper.TableName != combined || keeper.Severity != want.Severity)
            {
                keeper.ScriptId = pair.Min;
                keeper.ConflictingScriptId = pair.Max;
                keeper.TableName = combined;
                keeper.Severity = want.Severity;
                _db.Conflicts.Update(keeper);
            }

            handledDesiredPairs.Add(pair);
        }

        foreach (var kvp in desired)
        {
            var pair = kvp.Key;
            if (handledDesiredPairs.Contains(pair)) continue;

            var want = kvp.Value;
            var combined = ConflictKey.CombineTopics(want.Topics);
            var row = new Conflict
            {
                ScriptId = pair.Min,
                ConflictingScriptId = pair.Max,
                TableName = combined,
                Severity = want.Severity,
                DetectedAt = DateTime.UtcNow
            };
            await _db.Conflicts.AddAsync(row, cancellationToken);
            affectedScriptIds.Add(pair.Min);
            affectedScriptIds.Add(pair.Max);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var sid in affectedScriptIds)
            await ApplyConflictStatusForScriptAsync(sid, cancellationToken);
    }

    public async Task<bool> HasUnresolvedConflictsAsync(long scriptId, CancellationToken cancellationToken = default) =>
        await _db.Conflicts.AsNoTracking().AnyAsync(c =>
                !c.IsDeleted &&
                c.ResolvedAt == null &&
                (c.ScriptId == scriptId || c.ConflictingScriptId == scriptId),
            cancellationToken);

    public Task RecomputeScriptStatusAsync(long scriptId, CancellationToken cancellationToken = default) =>
        ApplyConflictStatusForScriptAsync(scriptId, cancellationToken);

    public async Task RecomputeScriptsAfterConflictChangeAsync(long scriptId, long otherScriptId, CancellationToken cancellationToken = default)
    {
        await ApplyConflictStatusForScriptAsync(scriptId, cancellationToken);
        if (otherScriptId != scriptId)
            await ApplyConflictStatusForScriptAsync(otherScriptId, cancellationToken);
    }

    public async Task RemoveOpenConflictWithDismissalAsync(long conflictId, long resolvedByUserId,
        ConflictCloseReason closeReason, CancellationToken cancellationToken = default)
    {
        var row = await _db.Conflicts.FirstOrDefaultAsync(c => c.Id == conflictId && !c.IsDeleted, cancellationToken);
        if (row == null || row.ResolvedAt != null) return;

        var min = Math.Min(row.ScriptId, row.ConflictingScriptId);
        var max = Math.Max(row.ScriptId, row.ConflictingScriptId);

        var sMin = await _db.Scripts.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == min && !s.IsDeleted, cancellationToken);
        var sMax = await _db.Scripts.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == max && !s.IsDeleted, cancellationToken);

        var now = DateTime.UtcNow;

        var priorSuppression = await _db.Conflicts
            .Where(c =>
                !c.IsDeleted &&
                c.ResolvedAt != null &&
                c.Id != row.Id &&
                c.ScriptId == min &&
                c.ConflictingScriptId == max &&
                c.ResolvedSqlHashScript != null &&
                c.ResolvedSqlHashConflictingScript != null)
            .ToListAsync(cancellationToken);
        foreach (var p in priorSuppression)
        {
            p.ResolvedSqlHashScript = null;
            p.ResolvedSqlHashConflictingScript = null;
            p.UpdatedAt = now;
            _db.Conflicts.Update(p);
        }

        row.ScriptId = min;
        row.ConflictingScriptId = max;
        row.ResolvedBy = resolvedByUserId;
        row.ResolvedAt = now;
        row.CloseReason = closeReason;
        row.UpdatedAt = now;
        if (sMin != null && sMax != null)
        {
            row.ResolvedSqlHashScript = ScriptSqlFingerprint.Compute(sMin);
            row.ResolvedSqlHashConflictingScript = ScriptSqlFingerprint.Compute(sMax);
        }
        else
        {
            row.ResolvedSqlHashScript = null;
            row.ResolvedSqlHashConflictingScript = null;
        }

        _db.Conflicts.Update(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task InvalidateDismissalsIfSqlChangedAsync(long scriptId, CancellationToken cancellationToken)
    {
        var list = await _db.Conflicts
            .Where(c =>
                !c.IsDeleted &&
                c.ResolvedAt != null &&
                c.ResolvedSqlHashScript != null &&
                c.ResolvedSqlHashConflictingScript != null &&
                (c.ScriptId == scriptId || c.ConflictingScriptId == scriptId))
            .ToListAsync(cancellationToken);

        foreach (var c in list)
        {
            var smin = await _db.Scripts.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == c.ScriptId && !s.IsDeleted, cancellationToken);
            var smax = await _db.Scripts.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == c.ConflictingScriptId && !s.IsDeleted, cancellationToken);
            if (smin == null || smax == null ||
                ScriptSqlFingerprint.Compute(smin) != c.ResolvedSqlHashScript ||
                ScriptSqlFingerprint.Compute(smax) != c.ResolvedSqlHashConflictingScript)
            {
                c.ResolvedSqlHashScript = null;
                c.ResolvedSqlHashConflictingScript = null;
                c.UpdatedAt = DateTime.UtcNow;
                _db.Conflicts.Update(c);
            }
        }
    }

    private async Task<bool> PairIsActivelyDismissedAsync(long minId, long maxId, CancellationToken cancellationToken)
    {
        var c = await _db.Conflicts.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    x.ResolvedAt != null &&
                    x.ResolvedSqlHashScript != null &&
                    x.ResolvedSqlHashConflictingScript != null &&
                    x.ScriptId == minId &&
                    x.ConflictingScriptId == maxId,
                cancellationToken);
        if (c == null) return false;

        var smin = await _db.Scripts.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == minId && !s.IsDeleted, cancellationToken);
        var smax = await _db.Scripts.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == maxId && !s.IsDeleted, cancellationToken);
        if (smin == null || smax == null) return false;

        return ScriptSqlFingerprint.Compute(smin) == c.ResolvedSqlHashScript &&
               ScriptSqlFingerprint.Compute(smax) == c.ResolvedSqlHashConflictingScript;
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
            var batchIdsInRelease = await GetBatchIdsInReleaseScopeAsync(releaseId.Value, cancellationToken);
            q = q.Where(s => s.BatchId.HasValue && batchIdsInRelease.Contains(s.BatchId.Value));
        }
        else
        {
            var rootId = await GetRootBatchIdAsync(script.BatchId.Value, cancellationToken);
            var batchIds = await GetSubtreeBatchIdsAsync(rootId, cancellationToken);
            q = q.Where(s => s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value));
        }

        return await q.ToListAsync(cancellationToken);
    }

    private async Task<HashSet<long>> GetBatchIdsInReleaseScopeAsync(long releaseId, CancellationToken cancellationToken)
    {
        var seeds = await _db.Batches.AsNoTracking()
            .Where(b => !b.IsDeleted && b.ReleaseId == releaseId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var result = new HashSet<long>(seeds);
        var frontier = new HashSet<long>(seeds);

        while (frontier.Count > 0)
        {
            var children = await _db.Batches.AsNoTracking()
                .Where(b =>
                    !b.IsDeleted &&
                    b.ParentBatchId.HasValue &&
                    frontier.Contains(b.ParentBatchId.Value) &&
                    (!b.ReleaseId.HasValue || b.ReleaseId.Value == releaseId))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            frontier.Clear();
            foreach (var id in children)
            {
                if (result.Add(id))
                    frontier.Add(id);
            }
        }

        return result;
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

        var hasOpen = await _db.Conflicts.AsNoTracking().AnyAsync(c =>
                !c.IsDeleted &&
                c.ResolvedAt == null &&
                (c.ScriptId == scriptId || c.ConflictingScriptId == scriptId),
            cancellationToken);

        if (hasOpen)
        {
            if (script.Status != ScriptStatus.Conflict)
            {
                script.StatusBeforeConflict = script.Status;
                script.Status = ScriptStatus.Conflict;
                _db.Scripts.Update(script);
            }
        }
        else if (script.Status == ScriptStatus.Conflict)
        {
            script.Status = script.StatusBeforeConflict ?? ScriptStatus.Draft;
            script.StatusBeforeConflict = null;
            _db.Scripts.Update(script);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NormalizeDuplicateOpenConflictsAsync(CancellationToken cancellationToken = default)
    {
        var open = await _db.Conflicts
            .Where(c => c.ResolvedAt == null && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var dupGroups = open
            .GroupBy(c => (
                Min: Math.Min(c.ScriptId, c.ConflictingScriptId),
                Max: Math.Max(c.ScriptId, c.ConflictingScriptId)))
            .Where(g => g.Count() > 1)
            .ToList();

        if (dupGroups.Count == 0) return;

        var affectedIds = new HashSet<long>();

        foreach (var g in dupGroups)
        {
            var pair = g.Key;
            var topics = new HashSet<string>(StringComparer.Ordinal);
            var severity = ConflictSeverity.ReviewAdvised;

            foreach (var c in g)
            {
                foreach (var t in ConflictKey.SplitStoredTopics(c.TableName))
                    topics.Add(t);
                if ((int)c.Severity < (int)severity)
                    severity = c.Severity;
            }

            var combined = ConflictKey.CombineTopics(topics);
            var ordered = g.OrderBy(c => c.Id).ToList();
            var keeper = ordered[0];

            keeper.ScriptId = pair.Min;
            keeper.ConflictingScriptId = pair.Max;
            keeper.TableName = combined;
            keeper.Severity = severity;
            _db.Conflicts.Update(keeper);
            affectedIds.Add(keeper.ScriptId);
            affectedIds.Add(keeper.ConflictingScriptId);

            foreach (var c in ordered.Skip(1))
            {
                affectedIds.Add(c.ScriptId);
                affectedIds.Add(c.ConflictingScriptId);
                _db.Conflicts.Remove(c);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var sid in affectedIds)
            await ApplyConflictStatusForScriptAsync(sid, cancellationToken);
    }

    private sealed class PairAgg
    {
        public HashSet<string> Topics { get; } = new(StringComparer.Ordinal);
        public ConflictSeverity Severity { get; set; } = ConflictSeverity.ReviewAdvised;
    }
}
