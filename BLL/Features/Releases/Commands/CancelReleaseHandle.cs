using BLL.Services;
using DAL.Context;
using DAL.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Releases.Commands;

/// <summary>
/// Sürümü silmez: iptal eder, tüm paketleri havuza döndürür (ReleaseId null, IsSeal açık),
/// sarmalayıcı kök batch'i soft-delete eder.
/// </summary>
public class CancelReleaseHandle : IRequestHandler<CancelReleaseRequest, CancelReleaseResponse>
{
    private readonly MyContext _db;
    private readonly IScriptConflictSyncService _conflictSync;

    public CancelReleaseHandle(MyContext db, IScriptConflictSyncService conflictSync)
    {
        _db = db;
        _conflictSync = conflictSync;
    }

    public async Task<CancelReleaseResponse> Handle(CancelReleaseRequest request, CancellationToken cancellationToken)
    {
        var ids = (request.ReleaseIds ?? Array.Empty<long>())
            .Concat(request.ReleaseId > 0 ? new[] { request.ReleaseId } : Array.Empty<long>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new CancelReleaseResponse
            {
                Success = false,
                Message = "İptal edilecek sürüm seçilmedi.",
                RequestedCount = 0,
                CancelledCount = 0
            };
        }

        var cancelled = 0;
        var failed = new List<long>();
        foreach (var releaseId in ids)
        {
            var ok = await CancelOneAsync(releaseId, cancellationToken);
            if (ok) cancelled++;
            else failed.Add(releaseId);
        }

        var message = failed.Count == 0
            ? $"{cancelled} sürüm iptal edildi; paketler havuza döndü ve kilidi açıldı."
            : $"{cancelled} sürüm iptal edildi, {failed.Count} sürümde işlem yapılamadı.";
        if (ids.Count == 1 && cancelled == 0)
            message = "Sürüm iptal edilemedi (bulunamadı veya zaten iptal).";

        return new CancelReleaseResponse
        {
            Success = cancelled > 0,
            Message = message,
            RequestedCount = ids.Count,
            CancelledCount = cancelled,
            FailedReleaseIds = failed
        };
    }

    private async Task<bool> CancelOneAsync(long releaseId, CancellationToken cancellationToken)
    {
        var release = await _db.Releases.FirstOrDefaultAsync(r => r.Id == releaseId && !r.IsDeleted,
            cancellationToken);
        if (release == null || release.IsCancelled)
            return false;

        var rootId = release.RootBatchId;
        var batches = await _db.Batches.Where(b => b.ReleaseId == releaseId && !b.IsDeleted)
            .ToListAsync(cancellationToken);
        var batchIds = batches.Select(b => b.Id).ToHashSet();

        var scriptIds = await _db.Scripts.AsNoTracking()
            .Where(s => s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value) && !s.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var b in batches)
        {
            b.ReleaseId = null;
            b.IsLocked = false;
            if (rootId.HasValue && b.ParentBatchId == rootId.Value)
                b.ParentBatchId = null;
            b.UpdatedAt = DateTime.UtcNow;
        }

        if (rootId.HasValue)
        {
            var wrap = batches.FirstOrDefault(x => x.Id == rootId.Value);
            if (wrap != null)
            {
                wrap.IsDeleted = true;
                wrap.UpdatedAt = DateTime.UtcNow;
            }
        }

        release.RootBatchId = null;
        release.IsCancelled = true;
        release.IsActive = false;
        release.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var sid in scriptIds.Distinct())
            await _conflictSync.SyncAfterScriptSavedAsync(sid, cancellationToken);

        return true;
    }
}
