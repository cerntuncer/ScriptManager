using BLL.Features.Releases.Commands;
using BLL.Features.Batchs.Commands;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Data;
using ScriptManager.Models.Release;
using ScriptManager.Security;
using System.IO;
using System.Linq;
using System.Text;

namespace ScriptManager.Controllers
{
    public class ReleasesController : Controller
    {
        private readonly MyContext _db;
        private readonly IMediator _mediator;

        public ReleasesController(MyContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Releases";
            ViewBag.IsAdmin = AuthHelper.IsAdmin(User);
            ViewBag.CanWrite = AuthHelper.CanWriteOperational(User);
            var vm = new ReleasesIndexViewModel
            {
                Releases = await ReleaseReadQueries.ListReleasesAsync(_db),
                Developers = await DeveloperReadQueries.ListOptionsAsync(_db)
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> OrphanRootBatches()
        {
            var roots = await _db.Batches.AsNoTracking()
                .Where(b => !b.IsDeleted && b.ReleaseId == null && b.ParentBatchId == null)
                .OrderBy(b => b.Name)
                .Select(b => new { id = b.Id, name = b.Name })
                .ToListAsync();
            return Json(new { roots });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateOrphanRoot([FromBody] CreateOrphanRootFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return BadRequest(new { success = false, message = "Klasör adı zorunludur." });

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
            var createdBy = body.CreatedBy;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                createdBy = uid;
            else if (createdBy <= 0)
                return BadRequest(new { success = false, message = "Oluşturan kullanıcıyı seçin." });

            var name = body.Name.Trim();
            var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == createdBy);
            if (!userExists)
                return BadRequest(new { success = false, message = "Geçersiz kullanıcı." });

            var exists = await BatchTreeHelper.SiblingNameExistsAsync(_db, null, null, name);
            if (exists)
                return BadRequest(new { success = false, message = "Bu isimde yetim kök klasör zaten var." });

            var root = new Batch
            {
                Name = name,
                ParentBatchId = null,
                ReleaseId = null,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _db.Batches.Add(root);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Yetim kök klasör oluşturuldu.", batchId = root.Id });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddFolder([FromBody] AddFolderFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();

            if (body == null || body.ParentBatchId <= 0 || string.IsNullOrWhiteSpace(body.Name))
                return BadRequest(new { success = false, message = "Üst klasör ve ad zorunludur." });

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);

            var createdBy = body.CreatedBy;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                createdBy = uid;
            else if (createdBy <= 0)
                return BadRequest(new { success = false, message = "Oluşturan zorunludur." });

            var parent = await _db.Batches.FirstOrDefaultAsync(b => b.Id == body.ParentBatchId && !b.IsDeleted);
            if (parent == null)
                return BadRequest(new { success = false, message = "Üst klasör bulunamadı." });

            var name = body.Name.Trim();
            var exists = await BatchTreeHelper.SiblingNameExistsAsync(_db, parent.Id, parent.ReleaseId, name);
            if (exists)
                return BadRequest(new { success = false, message = "Bu klasörde aynı isimde alt klasör var." });

            var batch = new Batch
            {
                ParentBatchId = parent.Id,
                ReleaseId = parent.ReleaseId,
                Name = name,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Alt klasör oluşturuldu.", batchId = batch.Id, batchName = batch.Name });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetTreeLock([FromBody] SetReleaseTreeLockFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();

            if (body == null || body.ReleaseId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var res = await _mediator.Send(new SetReleaseTreeLockRequest
            {
                ReleaseId = body.ReleaseId,
                Lock = body.Lock
            });

            if (!res.Success)
                return BadRequest(new { success = false, message = res.Message });

            return Json(new { success = true, message = res.Message });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateReleaseFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();

            if (body == null)
                return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Geçersiz istek gövdesi." });

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);

            var createdBy = body.CreatedBy;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                createdBy = uid;
            else if (createdBy <= 0)
                return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Oluşturan kullanıcıyı seçin." });

            if (string.IsNullOrWhiteSpace(body.Name))
                return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Release adı girin." });
            if (string.IsNullOrWhiteSpace(body.Version))
                return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Versiyon girin." });

            if (await _db.Releases.AnyAsync(r => !r.IsDeleted && r.Version == body.Version))
                return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Bu versiyon zaten kullanılıyor." });

            var rootMode = (body.RootMode ?? "new").Trim().ToLowerInvariant();
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var release = new DAL.Entities.Release
                {
                    Name = body.Name.Trim(),
                    Version = body.Version.Trim(),
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    IsActive = true
                };
                _db.Releases.Add(release);
                await _db.SaveChangesAsync();

                if (rootMode == "existing")
                {
                    var rootId = body.ExistingRootBatchId ?? 0;
                    if (rootId <= 0)
                        return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Mevcut kök klasör seçin." });
                    if (!await BatchTreeHelper.IsOrphanRootAsync(_db, rootId))
                        return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Seçilen kayıt kök yetim klasör değil." });
                    if (!await BatchTreeHelper.EntireSubtreeIsOrphanAsync(_db, rootId))
                        return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Seçilen ağaç başka release'e bağlı." });

                    await BatchTreeHelper.PropagateReleaseIdAsync(_db, rootId, release.Id);
                    release.RootBatchId = rootId;
                }
                else
                {
                    var rootName = (body.NewRootBatchName ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(rootName))
                        return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Yeni kök klasör adı girin." });
                    if (await BatchTreeHelper.SiblingNameExistsAsync(_db, null, release.Id, rootName))
                        return BadRequest(new CreateReleaseJsonResponse { Success = false, Message = "Bu isimde kök klasör zaten var." });

                    var root = new Batch
                    {
                        Name = rootName,
                        ParentBatchId = null,
                        ReleaseId = release.Id,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _db.Batches.Add(root);
                    await _db.SaveChangesAsync();
                    release.RootBatchId = root.Id;
                }

                _db.Releases.Update(release);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var batchIds = await _db.Batches.AsNoTracking()
                    .Where(b => !b.IsDeleted && b.ReleaseId == release.Id)
                    .Select(b => b.Id)
                    .ToListAsync();
                var scriptCount = batchIds.Count == 0
                    ? 0
                    : await _db.Scripts.CountAsync(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted && s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value));
                var rbCount = batchIds.Count == 0
                    ? 0
                    : await _db.Scripts.CountAsync(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted && s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value) && !string.IsNullOrWhiteSpace(s.RollbackScript));

                return Json(new CreateReleaseJsonResponse
                {
                    Success = true,
                    Message = "Release oluşturuldu.",
                    ReleaseId = release.Id,
                    Version = release.Version,
                    CreatedAtDisplay = release.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    ScriptCount = scriptCount,
                    RollbackScriptCount = rbCount,
                    RootBatchId = release.RootBatchId
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IActionResult> Detail(long id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var detail = await ReleaseReadQueries.GetReleaseDetailAsync(_db, id);
            if (detail == null || !detail.Success)
                return NotFound();

            ViewBag.Developers = await DeveloperReadQueries.ListOptionsAsync(_db);
            ViewBag.IsAdmin = AuthHelper.IsAdmin(User);
            ViewBag.CanWrite = AuthHelper.CanWriteOperational(User);
            ViewBag.CurrentUserId = await AuthHelper.GetActorUserIdAsync(User, _db);
            ViewData["Title"] = $"{detail.ReleaseName} — {detail.Version}";
            return View(detail);
        }

        /// <summary>Ajax ile detay gövdesini yenilemek için (klasör listesi, script tablosu, birleşik SQL).</summary>
        [HttpGet]
        public async Task<IActionResult> DetailRefresh(long id)
        {
            if (id <= 0)
                return NotFound();

            var detail = await ReleaseReadQueries.GetReleaseDetailAsync(_db, id);
            if (detail == null || !detail.Success)
                return NotFound();

            ViewBag.Developers = await DeveloperReadQueries.ListOptionsAsync(_db);
            ViewBag.IsAdmin = AuthHelper.IsAdmin(User);
            ViewBag.CanWrite = AuthHelper.CanWriteOperational(User);
            ViewBag.CurrentUserId = await AuthHelper.GetActorUserIdAsync(User, _db);
            return PartialView("_ReleaseDetailRefresh", detail);
        }

        /// <summary>Seçili scriptlerin ileri SQL'ini tek dosya olarak indirir (sıra = sürüm post-order).</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ExportSelectedSql([FromBody] ExportSelectedSqlRequest? body)
        {
            if (body == null || body.ReleaseId <= 0)
                return BadRequest("Geçersiz istek.");

            var detail = await ReleaseReadQueries.GetReleaseDetailAsync(_db, body.ReleaseId);
            if (detail == null || !detail.Success)
                return NotFound();
            if (detail.IsCancelled)
                return BadRequest("İptal edilmiş sürüm.");

            var requested = (body.ScriptIds ?? []).Where(id => id > 0).Distinct().ToList();
            if (requested.Count == 0)
                return BadRequest("En az bir script seçin.");

            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
            {
                var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
                var allowed = detail.Scripts.Where(s => s.DeveloperId == uid).Select(s => s.ScriptId).ToHashSet();
                requested = requested.Where(allowed.Contains).ToList();
                if (requested.Count == 0)
                    return Forbid();
            }

            var export = ReleaseReadQueries.BuildExportForScriptSubset(detail, requested);
            if (export == null)
                return BadRequest("Seçilen scriptler bu sürümde bulunamadı.");

            var (sql, _) = export.Value;
            var safeVersion = string.Join("_", (detail.Version ?? "release").Split(Path.GetInvalidFileNameChars()));
            var bytes = Encoding.UTF8.GetBytes(sql);
            return File(bytes, "application/sql", $"{safeVersion}-selected.sql");
        }

        /// <summary>Toplu iptal; sürüm silinmez, paketler havuza döner.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkCancel([FromBody] CancelReleaseFormRequest? body)
        {
            if (!AuthHelper.CanDeleteRelease(User))
                return Forbid();

            var ids = body?.ReleaseIds?.Where(i => i > 0).Distinct().ToList() ?? new List<long>();
            if (ids.Count == 0)
                return BadRequest(new { success = false, message = "Sürüm seçilmedi." });

            var res = await _mediator.Send(new CancelReleaseRequest { ReleaseIds = ids });
            return Json(new
            {
                success = res.Success,
                message = res.Message,
                requestedCount = res.RequestedCount,
                cancelledCount = res.CancelledCount,
                failedReleaseIds = res.FailedReleaseIds
            });
        }

        /// <summary>Tekil iptal; paketler havuza döner ve kilidi açılır.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CancelOne([FromBody] CancelReleaseFormRequest? body)
        {
            if (!AuthHelper.CanDeleteRelease(User))
                return Forbid();

            if (body == null || body.ReleaseId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz sürüm." });

            var res = await _mediator.Send(new CancelReleaseRequest { ReleaseId = body.ReleaseId });
            if (!res.Success)
                return BadRequest(new { success = false, message = res.Message });

            return Json(new { success = true, message = res.Message });
        }
    }
}
