using BLL.Features.Releases.Commands;
using DAL.Context;
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

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddFolder([FromBody] AddFolderFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();

            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return BadRequest(CreateReleaseBatchResponse.Fail("Ad zorunludur."));

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);

            var createdBy = body.CreatedBy;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                createdBy = uid;
            else if (createdBy <= 0)
                return BadRequest(CreateReleaseBatchResponse.Fail("Oluşturan zorunludur."));

            var result = await _mediator.Send(new CreateReleaseBatchRequest
            {
                ParentBatchId = body.ParentBatchId,
                ReleaseId = body.ReleaseId,
                Name = body.Name.Trim(),
                CreatedBy = createdBy
            });

            if (!result.Success)
                return BadRequest(result);

            return Json(result);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateReleaseFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();

            if (body == null)
                return BadRequest(CreateReleaseResponse.Fail("Geçersiz istek gövdesi."));

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);

            var createdBy = body.CreatedBy;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                createdBy = uid;
            else if (createdBy <= 0)
                return BadRequest(CreateReleaseResponse.Fail("Oluşturan kullanıcıyı seçin."));

            long? restrictDev = null;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                restrictDev = uid;

            var result = await _mediator.Send(new CreateReleaseRequest
            {
                Name = body.Name,
                Version = body.Version,
                CreatedBy = createdBy,
                InitialBatchName = body.InitialBatchName,
                SelectedScriptIds = body.SelectedScriptIds,
                RootScriptIds = body.RootScriptIds,
                Folders = body.Folders,
                RestrictScriptAssignmentToDeveloperId = restrictDev
            });

            if (!result.Success)
                return BadRequest(result);

            return Json(result);
        }

        /// <summary>Başka batch'teki scripti bu release içindeki bir hedef batch'e taşır.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AssignScriptToBatch([FromBody] AssignScriptToBatchFormRequest? body)
        {
            if (!AuthHelper.CanWriteOperational(User))
                return Forbid();

            if (body == null || body.ReleaseId <= 0 || body.ScriptId <= 0 || body.TargetBatchId <= 0)
                return BadRequest(new { success = false, message = "Release, script ve hedef klasör zorunludur." });

            var release = await _db.Releases.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == body.ReleaseId && !r.IsDeleted);
            if (release == null)
                return BadRequest(new { success = false, message = "Release bulunamadı." });

            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
            {
                var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
                var owns = await _db.Scripts.AsNoTracking()
                    .AnyAsync(s => s.Id == body.ScriptId && s.DeveloperId == uid);
                if (!owns)
                    return Forbid();
            }

            var assignRes = await _mediator.Send(new MoveScriptToReleaseBatchRequest
            {
                ReleaseId = body.ReleaseId,
                ScriptId = body.ScriptId,
                TargetBatchId = body.TargetBatchId
            });
            if (!assignRes.Success)
                return BadRequest(new { success = false, message = assignRes.Message ?? "Taşıma başarısız." });

            return Json(assignRes);
        }

        public async Task<IActionResult> Detail(long id)
        {
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

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteReleasesRequest? body)
        {
            if (!AuthHelper.CanDeleteRelease(User))
                return Forbid();

            var ids = body?.ReleaseIds?.Where(i => i > 0).Distinct().ToList() ?? new List<long>();
            if (ids.Count == 0)
                return BadRequest(new { success = false, message = "Sürüm seçilmedi." });

            var actor = await AuthHelper.GetActorUserIdAsync(User, _db);
            var bulkRes = await _mediator.Send(new DeleteReleasesRequest
            {
                ReleaseIds = ids,
                ActorUserId = actor
            });

            return Json(bulkRes);
        }

        /// <summary>Tekil sürüm silme (cascade); yönetici.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteOne([FromBody] ReleaseDeleteBody? body)
        {
            if (!AuthHelper.CanDeleteRelease(User))
                return Forbid();

            if (body == null || body.ReleaseId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz sürüm." });

            var actor = await AuthHelper.GetActorUserIdAsync(User, _db);
            var delRes = await _mediator.Send(new DeleteReleaseRequest
            {
                ReleaseId = body.ReleaseId,
                ActorUserId = actor
            });
            if (!delRes.Success)
                return NotFound(new { success = false, message = delRes.Message });

            return Json(delRes);
        }
    }
}
