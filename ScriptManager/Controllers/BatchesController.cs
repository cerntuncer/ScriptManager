using BLL.Features.Batchs;
using BLL.Features.Batchs.Commands;
using DAL.Context;
using DAL.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Data;
using ScriptManager.Models.Release;
using ScriptManager.Security;

namespace ScriptManager.Controllers;

public class BatchesController : Controller
{
    private readonly MyContext _db;
    private readonly IMediator _mediator;

    public BatchesController(MyContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Versiyonlar";
        ViewBag.CanWrite = AuthHelper.CanWriteOperational(User);
        ViewBag.Developers = await DeveloperReadQueries.ListOptionsAsync(_db);
        var tree = await PoolBatchQueries.GetPoolBatchTreeAsync(_db);
        return View(tree);
    }

    /// <summary>
    /// Havuz: releaseId yok; parentBatchId 0 = kökler.
    /// Release (düzenleme): releaseId dolu; parentBatchId 0 = sürüm kökünün bir altı.
    /// </summary>
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TreeChildren(long? releaseId, long parentBatchId = 0)
    {
        var scriptBatchSet = (await _db.Scripts.AsNoTracking()
            .Where(s => s.BatchId != null && !s.IsDeleted && s.Status != ScriptStatus.Deleted)
            .Select(s => s.BatchId!.Value)
            .Distinct()
            .ToListAsync()).ToHashSet();

        var activeReleaseIds = await _db.Releases.AsNoTracking()
            .Where(r => !r.IsDeleted && !r.IsCancelled)
            .Select(r => r.Id)
            .ToHashSetAsync();

        List<(long Id, string Name, bool IsLocked, long? BatchReleaseId)> list;

        if (releaseId is > 0)
        {
            var rel = await _db.Releases.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == releaseId && !r.IsDeleted);
            if (rel == null || rel.IsCancelled)
                return Json(new { children = Array.Empty<object>() });

            List<(long Id, string Name, bool IsLocked, long? BatchReleaseId)> rawTuples;
            if (parentBatchId <= 0)
            {
                if (rel.RootBatchId.HasValue)
                {
                    var raw = await _db.Batches.AsNoTracking()
                        .Where(b =>
                            !b.IsDeleted && b.ReleaseId == releaseId && b.ParentBatchId == rel.RootBatchId.Value)
                        .OrderBy(b => b.Name).ThenBy(b => b.Id)
                        .Select(b => new { b.Id, b.Name, b.IsLocked, b.ReleaseId })
                        .ToListAsync();
                    rawTuples = raw.Select(x => (x.Id, x.Name, x.IsLocked, x.ReleaseId)).ToList();
                }
                else
                {
                    var raw = await _db.Batches.AsNoTracking()
                        .Where(b => !b.IsDeleted && b.ReleaseId == releaseId && b.ParentBatchId == null)
                        .OrderBy(b => b.Name).ThenBy(b => b.Id)
                        .Select(b => new { b.Id, b.Name, b.IsLocked, b.ReleaseId })
                        .ToListAsync();
                    rawTuples = raw.Select(x => (x.Id, x.Name, x.IsLocked, x.ReleaseId)).ToList();
                }
            }
            else
            {
                var p = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == parentBatchId && !b.IsDeleted);
                if (p == null || p.ReleaseId != releaseId)
                    return Json(new { children = Array.Empty<object>() });
                var raw = await _db.Batches.AsNoTracking()
                    .Where(b => !b.IsDeleted && b.ReleaseId == releaseId && b.ParentBatchId == parentBatchId)
                    .OrderBy(b => b.Name).ThenBy(b => b.Id)
                    .Select(b => new { b.Id, b.Name, b.IsLocked, b.ReleaseId })
                    .ToListAsync();
                rawTuples = raw.Select(x => (x.Id, x.Name, x.IsLocked, x.ReleaseId)).ToList();
            }

            list = rawTuples;
        }
        else
        {
            if (parentBatchId <= 0)
            {
                var poolRoots = await _db.Batches.AsNoTracking()
                    .Where(b => !b.IsDeleted && b.ReleaseId == null && b.ParentBatchId == null)
                    .Select(b => new { b.Id, b.Name, b.IsLocked, b.ReleaseId })
                    .ToListAsync();
                var relRoots = await _db.Batches.AsNoTracking()
                    .Where(b =>
                        !b.IsDeleted && b.ParentBatchId == null && b.ReleaseId != null &&
                        activeReleaseIds.Contains(b.ReleaseId.Value))
                    .Select(b => new { b.Id, b.Name, b.IsLocked, b.ReleaseId })
                    .ToListAsync();
                list = poolRoots.Concat(relRoots)
                    .OrderBy(x => x.Name).ThenBy(x => x.Id)
                    .Select(x => (x.Id, x.Name, x.IsLocked, x.ReleaseId))
                    .ToList();
            }
            else
            {
                var parent = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == parentBatchId && !b.IsDeleted);
                if (parent == null)
                    return Json(new { children = Array.Empty<object>() });

                var scopeRid = parent.ReleaseId;
                var raw = await _db.Batches.AsNoTracking()
                    .Where(b =>
                        !b.IsDeleted && b.ParentBatchId == parentBatchId &&
                        (scopeRid == null ? b.ReleaseId == null : b.ReleaseId == scopeRid))
                    .OrderBy(b => b.Name).ThenBy(b => b.Id)
                    .Select(b => new { b.Id, b.Name, b.IsLocked, b.ReleaseId })
                    .ToListAsync();
                list = raw.Select(x => (x.Id, x.Name, x.IsLocked, x.ReleaseId)).ToList();
            }
        }

        var ids = list.Select(x => x.Id).ToList();
        var hasChildren = await _db.Batches.AsNoTracking()
            .Where(b => !b.IsDeleted && b.ParentBatchId != null && ids.Contains(b.ParentBatchId.Value))
            .Select(b => b.ParentBatchId!.Value)
            .Distinct()
            .ToHashSetAsync();

        var rows = new List<object>();
        foreach (var item in list)
        {
            var hasCh = hasChildren.Contains(item.Id);
            var inLockedRelease = (releaseId is null || releaseId <= 0) && item.BatchReleaseId.HasValue &&
                                  activeReleaseIds.Contains(item.BatchReleaseId.Value);
            var canAddScript = !item.IsLocked && !inLockedRelease;
            var canAddChild = !item.IsLocked && !scriptBatchSet.Contains(item.Id) && !inLockedRelease;
            var canPkg = false;
            if (!item.BatchReleaseId.HasValue && (releaseId is null || releaseId <= 0))
            {
                var v = await PoolBatchRules.ValidateSubtreeReadyForReleaseAsync(_db, item.Id);
                canPkg = v.Ok;
            }

            rows.Add(new
            {
                batchId = item.Id,
                name = item.Name,
                isLocked = item.IsLocked,
                hasChildren = hasCh,
                canAddScript,
                canAddChild,
                canPackageRelease = canPkg
            });
        }

        return Json(new { children = rows });
    }

    /// <summary>Kökten yaprağa doğru kimlik sırası (script/release sihirbazında ön konumlama için).</summary>
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BatchPath(long batchId, long? releaseId = null)
    {
        var batch = await _db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
        if (batch == null)
            return Json(new { path = Array.Empty<object>() });

        if (releaseId is > 0)
        {
            if (batch.ReleaseId != releaseId)
                return Json(new { path = Array.Empty<object>() });

            var rel = await _db.Releases.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == releaseId.Value && !r.IsDeleted);
            if (rel == null || rel.IsCancelled)
                return Json(new { path = Array.Empty<object>() });

            var chain = new List<(long Id, string Name)>();
            var cur = batch;
            var guard = 0;
            while (cur != null && guard++ < 256)
            {
                chain.Add((cur.Id, cur.Name ?? ""));
                if (rel.RootBatchId.HasValue)
                {
                    if (cur.Id == rel.RootBatchId.Value)
                        break;
                }
                else if (!cur.ParentBatchId.HasValue)
                    break;

                if (!cur.ParentBatchId.HasValue)
                    return Json(new { path = Array.Empty<object>() });

                cur = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == cur.ParentBatchId.Value && !b.IsDeleted);
                if (cur == null || cur.ReleaseId != releaseId)
                    return Json(new { path = Array.Empty<object>() });
            }

            chain.Reverse();
            return Json(new { path = chain.Select(p => new { id = p.Id, name = p.Name }) });
        }

        {
            var chain = new List<(long Id, string Name)>();
            var cur = batch;
            var guard = 0;
            while (cur != null && guard++ < 256)
            {
                chain.Add((cur.Id, cur.Name ?? ""));
                if (!cur.ParentBatchId.HasValue)
                    break;
                cur = await _db.Batches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == cur.ParentBatchId.Value && !b.IsDeleted);
            }

            chain.Reverse();
            return Json(new { path = chain.Select(p => new { id = p.Id, name = p.Name }) });
        }
    }

    /// <summary>Yalnızca havuz (ReleaseId null) ağacına alt veya kök batch ekler.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddPoolFolder([FromBody] AddFolderFormRequest? body)
    {
        if (!AuthHelper.CanWriteOperational(User))
            return Forbid();

        if (body == null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { success = false, message = "Ad zorunludur." });

        var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
        var createdBy = body.CreatedBy;
        if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
            createdBy = uid;
        else if (createdBy <= 0)
            return BadRequest(new { success = false, message = "Oluşturan zorunludur." });

        if (body.ParentBatchId > 0)
        {
            var parent = await _db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == body.ParentBatchId && !b.IsDeleted);
            if (parent == null)
                return BadRequest(new { success = false, message = "Üst klasör bulunamadı." });
            if (parent.ReleaseId != null)
                return BadRequest(new { success = false, message = "Yalnızca havuz klasörlerine ekleme yapılır." });
        }

        var result = await _mediator.Send(new CreateBatchRequest
        {
            ParentBatchId = body.ParentBatchId,
            ReleaseId = null,
            Name = body.Name.Trim(),
            CreatedBy = createdBy
        });

        if (!result.Success)
            return BadRequest(result);

        return Json(result);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeletePoolBatch([FromBody] DeletePoolBatchBody? body)
    {
        if (!AuthHelper.CanWriteOperational(User))
            return Forbid();

        var batchId = body?.BatchId ?? 0;
        if (batchId <= 0)
            return BadRequest(new { success = false, message = "Geçersiz versiyon." });

        var root = await _db.Batches
            .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);

        if (root == null)
            return BadRequest(new { success = false, message = "Versiyon bulunamadı." });
        if (root.ParentBatchId != null)
            return BadRequest(new { success = false, message = "Yalnızca kök versiyonlar silinebilir." });
        if (root.IsLocked)
            return BadRequest(new { success = false, message = "Kilitli versiyon silinemez." });
        if (root.ReleaseId != null)
            return BadRequest(new { success = false, message = "Bir sürüme bağlı versiyon silinemez." });

        // Tüm alt batch ID'lerini topla
        var allBatches = await _db.Batches.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new { b.Id, b.ParentBatchId })
            .ToListAsync();

        var toDelete = new HashSet<long> { batchId };
        var queue = new Queue<long>();
        queue.Enqueue(batchId);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var child in allBatches.Where(b => b.ParentBatchId == cur))
            {
                if (toDelete.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        // Scriptleri soft-delete
        var scripts = await _db.Scripts
            .Where(s => s.BatchId.HasValue && toDelete.Contains(s.BatchId.Value) && !s.IsDeleted)
            .ToListAsync();
        foreach (var s in scripts)
            s.IsDeleted = true;

        // Batch'leri soft-delete
        var batches = await _db.Batches
            .Where(b => toDelete.Contains(b.Id) && !b.IsDeleted)
            .ToListAsync();
        foreach (var b in batches)
            b.IsDeleted = true;

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = $"\"{root.Name}\" versiyonu ve içeriği silindi." });
    }
}

public class DeletePoolBatchBody
{
    public long BatchId { get; set; }
}
