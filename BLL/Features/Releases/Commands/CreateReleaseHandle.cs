using BLL.Features.Batchs;
using BLL.Services;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Releases.Commands
{
    /// <summary>
    /// Yalnızca Release kaydı oluşturur; seçilen havuz paketleri (ve alt ağaçları) aynı hiyerarşiyle
    /// bu sürüme bağlanır — ekstra üst batch oluşturulmaz.
    /// </summary>
    public class CreateReleaseHandle : IRequestHandler<CreateReleaseRequest, CreateReleaseResponse>
    {
        private readonly MyContext _db;
        private readonly IScriptConflictSyncService _conflictSync;

        public CreateReleaseHandle(MyContext db, IScriptConflictSyncService conflictSync)
        {
            _db = db;
            _conflictSync = conflictSync;
        }

        public async Task<CreateReleaseResponse> Handle(CreateReleaseRequest request,
            CancellationToken cancellationToken)
        {
            var name = string.IsNullOrWhiteSpace(request.Name) ? "Release" : request.Name.Trim();
            var version = string.IsNullOrWhiteSpace(request.Version) ? "0.0.0" : request.Version.Trim();

            var rootIds = (request.SourcePoolBatchRootIds ?? new List<long>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            if (rootIds.Count == 0)
                return CreateReleaseResponse.Fail("Release için en az bir havuz paketi seçin.");

            var allFlat = await _db.Batches.AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Select(b => new { b.Id, b.ParentBatchId })
                .ToListAsync(cancellationToken);
            var edges = allFlat.Select(b => (b.Id, b.ParentBatchId)).ToList();

            foreach (var rid in rootIds)
            {
                var b = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == rid && !x.IsDeleted, cancellationToken);
                if (b == null)
                    return CreateReleaseResponse.Fail("Seçilen batch bulunamadı.");
                if (b.ReleaseId != null)
                    return CreateReleaseResponse.Fail($"\"{b.Name}\" zaten bir release'e bağlı.");
                if (b.IsLocked)
                    return CreateReleaseResponse.Fail($"\"{b.Name}\" kilitli.");

                var (ok, err) = await PoolBatchRules.ValidateSubtreeReadyForReleaseAsync(_db, rid, cancellationToken);
                if (!ok)
                    return CreateReleaseResponse.Fail(err ?? "Seçilen paket release için hazır değil.");

                if (request.RestrictScriptAssignmentToDeveloperId is long devId && devId > 0)
                {
                    var subtree = new HashSet<long> { rid };
                    foreach (var d in PoolBatchRules.CollectDescendingIds(edges, rid))
                        subtree.Add(d);
                    var bad = await _db.Scripts.AsNoTracking()
                        .AnyAsync(s =>
                                s.BatchId.HasValue && subtree.Contains(s.BatchId.Value) && !s.IsDeleted &&
                                s.Status != ScriptStatus.Deleted && s.DeveloperId != devId,
                            cancellationToken);
                    if (bad)
                        return CreateReleaseResponse.Fail("Bu pakette size ait olmayan scriptler var.");
                }
            }

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var release = new Release
                {
                    Name = name,
                    Version = version,
                    CreatedBy = request.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsCancelled = false,
                    IsDeleted = false,
                    RootBatchId = null
                };
                _db.Releases.Add(release);
                await _db.SaveChangesAsync(cancellationToken);

                var syncScriptIds = new List<long>();

                foreach (var poolRootId in rootIds)
                {
                    var descendants = PoolBatchRules.CollectDescendingIds(edges, poolRootId);
                    var poolRoot = await _db.Batches.FirstAsync(x => x.Id == poolRootId, cancellationToken);
                    poolRoot.ReleaseId = release.Id;
                    poolRoot.IsLocked = true;
                    _db.Batches.Update(poolRoot);

                    foreach (var did in descendants)
                    {
                        var d = await _db.Batches.FirstAsync(x => x.Id == did, cancellationToken);
                        d.ReleaseId = release.Id;
                        d.IsLocked = true;
                        _db.Batches.Update(d);
                    }

                    var affectedBatches = new List<long> { poolRootId };
                    affectedBatches.AddRange(descendants);
                    var sids = await _db.Scripts.AsNoTracking()
                        .Where(s =>
                            s.BatchId.HasValue && affectedBatches.Contains(s.BatchId.Value) && !s.IsDeleted &&
                            s.Status != ScriptStatus.Deleted)
                        .Select(s => s.Id)
                        .ToListAsync(cancellationToken);
                    syncScriptIds.AddRange(sids);
                }

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                foreach (var sid in syncScriptIds.Distinct())
                    await _conflictSync.SyncAfterScriptSavedAsync(sid);

                var batchIds = await _db.Batches.AsNoTracking()
                    .Where(b => b.ReleaseId == release.Id && !b.IsDeleted)
                    .Select(b => b.Id)
                    .ToListAsync(cancellationToken);
                var scriptCount = batchIds.Count == 0
                    ? 0
                    : await _db.Scripts.CountAsync(s =>
                        s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value) && !s.IsDeleted &&
                        s.Status != ScriptStatus.Deleted, cancellationToken);
                var rbCount = batchIds.Count == 0
                    ? 0
                    : await _db.Scripts.CountAsync(s =>
                        s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value) && !s.IsDeleted &&
                        s.Status != ScriptStatus.Deleted
                        && !string.IsNullOrWhiteSpace(s.RollbackScript), cancellationToken);

                return new CreateReleaseResponse
                {
                    Success = true,
                    Message = "Release oluşturuldu; seçilen paketler bu sürüme bağlandı ve kilitlendi.",
                    ReleaseId = release.Id,
                    Version = release.Version,
                    ReleaseName = release.Name,
                    CreatedAtDisplay = release.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    ScriptCount = scriptCount,
                    RollbackScriptCount = rbCount,
                    RootBatchId = null
                };
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
