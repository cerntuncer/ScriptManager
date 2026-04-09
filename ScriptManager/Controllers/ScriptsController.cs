using BLL.Features.Scripts.Commands;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Data;
using ScriptManager.Models.Script;
using ScriptManager.Security;

namespace ScriptManager.Controllers
{
    public class ScriptsController : Controller
    {
        private readonly MyContext _db;
        private readonly IMediator _mediator;

        public ScriptsController(MyContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Scripts";
            ViewBag.CanAuthorScripts = AuthHelper.CanAuthorScripts(User);
            var scripts = await ScriptReadQueries.ListActiveScriptsAsync(_db);
            return View(new ScriptsIndexViewModel { Scripts = scripts });
        }

        [HttpGet]
        public async Task<IActionResult> DeveloperOptions()
        {
            var developers = await DeveloperReadQueries.ListOptionsAsync(_db);
            return Json(new
            {
                developers = developers.Select(d => new { id = d.UserId, name = d.Name, email = d.Email })
            });
        }

        /// <summary>Release / batch modallarında mevcut script çoklu seçimi.</summary>
        [HttpGet]
        public async Task<IActionResult> ListForPicker()
        {
            var scripts = await _db.Scripts.AsNoTracking()
                .Include(s => s.Batch)
                .ThenInclude(b => b!.Release)
                .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted && s.Status == ScriptStatus.Ready)
                .OrderBy(s => s.Name)
                .ThenBy(s => s.Id)
                .ToListAsync();

            var rows = scripts.Select(s =>
            {
                var loc = s.Batch == null
                    ? "Atanmamış"
                    : s.Batch.Release != null
                        ? $"{s.Batch.Release.Version} — {s.Batch.Name}"
                        : s.Batch.Name;
                return new { id = s.Id, name = s.Name, label = loc };
            }).ToList();

            return Json(new { scripts = rows });
        }

        /// <summary>Script oluşturma: havuz batch ağacı ve geliştiriciler.</summary>
        [HttpGet]
        public async Task<IActionResult> CreateWizardContext()
        {
            var developers = await DeveloperReadQueries.ListOptionsAsync(_db);
            var poolBatchRoots = await PoolBatchQueries.GetPoolBatchTreeAsync(_db);

            return Json(new
            {
                developers = developers.Select(d => new { id = d.UserId, name = d.Name, email = d.Email }),
                poolBatchRoots
            });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            var script = await _db.Scripts.AsNoTracking()
                .Include(s => s.Batch)
                .Include(s => s.Developer)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.Status != ScriptStatus.Deleted);

            if (script == null)
                return NotFound();

            var model = ScriptReadQueries.ToListItem(script);
            ViewBag.CanDelete = AuthHelper.CanDeleteScript(User, model.DeveloperId);

            var actorId = await AuthHelper.GetActorUserIdAsync(User, _db);
            var actor = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId && !u.IsDeleted);
            ViewBag.ActorRole = actor?.Role;
            ViewBag.ActorUserId = actorId;
            ViewBag.CanChangeScriptStatus = actor != null &&
                (actor.Role == UserRole.Admin ||
                 actor.Role == UserRole.Developer ||
                 actor.Role == UserRole.Tester);

            ViewData["Title"] = $"Script — {model.Name}";
            return View(model);
        }

        /// <summary>Taslak -> İncelemede / Hazır; İncelemede -> Hazır durum geçişi.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeScriptStatusFormRequest? body)
        {
            if (body == null || body.ScriptId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            if (!Enum.IsDefined(typeof(ScriptStatus), body.NewStatus))
                return BadRequest(new { success = false, message = "Geçersiz durum." });

            var actorId = await AuthHelper.GetActorUserIdAsync(User, _db);
            if (actorId <= 0)
                return BadRequest(new { success = false, message = "Oturum kullanıcısı bulunamadı." });

            var result = await _mediator.Send(new UpdateScriptRequest
            {
                ScriptId = body.ScriptId,
                UserId = actorId,
                Status = body.NewStatus
            });

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            var newStatus = (ScriptStatus)body.NewStatus;

            return Json(new
            {
                success = true,
                message = result.Message ?? "Durum güncellendi.",
                status = newStatus.ToString(),
                statusDisplay = ScriptReadQueries.ScriptStatusDisplay(newStatus)
            });
        }

        [HttpGet]
        public async Task<IActionResult> ScriptSql(long id)
        {
            var script = await _db.Scripts.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (script == null)
                return NotFound(new { message = "Script bulunamadı." });

            return Json(new { sqlScript = script.SqlScript, rollbackScript = script.RollbackScript });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateScriptFormRequest? body)
        {
            if (!AuthHelper.CanAuthorScripts(User))
                return Forbid();

            if (body == null)
                return BadRequest(new ApiMutationResponse { Success = false, Message = "Geçersiz istek gövdesi." });

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);

            if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.SqlScript))
                return BadRequest(new ApiMutationResponse { Success = false, Message = "Ad ve SQL zorunludur." });

            var developerId = body.DeveloperId;
            if (AuthHelper.IsDeveloper(User) && !AuthHelper.IsAdmin(User))
                developerId = uid;
            else if (developerId <= 0)
                return BadRequest(new ApiMutationResponse { Success = false, Message = "Geliştirici seçin." });

            var medReq = new CreateScriptRequest
            {
                Name = body.Name.Trim(),
                SqlScript = body.SqlScript,
                RollbackScript = string.IsNullOrWhiteSpace(body.RollbackScript) ? null : body.RollbackScript,
                DeveloperId = developerId,
                ActorUserId = uid,
                BatchId = body.BatchId
            };

            var result = await _mediator.Send(medReq);

            if (!result.Success)
                return BadRequest(new ApiMutationResponse { Success = false, Message = result.Message ?? "Kayıt başarısız." });

            var batchDisplay = result.BatchName ?? "Atanmamış";
            if (!string.IsNullOrEmpty(result.ReleaseVersion))
                batchDisplay = $"{result.ReleaseVersion} — {batchDisplay}";

            return Json(new ApiMutationResponse
            {
                Success = true,
                Message = result.Message ?? "Script oluşturuldu.",
                ScriptId = result.ScriptId,
                BatchId = result.BatchId,
                ScriptName = medReq.Name,
                Status = ScriptReadQueries.ScriptStatusDisplay(ScriptStatus.Draft),
                BatchName = batchDisplay,
                DeveloperName = result.DeveloperName ?? string.Empty,
                HasRollback = !string.IsNullOrWhiteSpace(medReq.RollbackScript),
                CreatedAtDisplay = DateTime.UtcNow.ToLocalTime().ToString("dd.MM.yyyy"),
                CanDelete = AuthHelper.CanDeleteScript(User, developerId)
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var script = await _db.Scripts.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (script == null || script.Status == ScriptStatus.Deleted)
                return NotFound(new { success = false, message = "Script bulunamadı." });

            if (!AuthHelper.CanDeleteScript(User, script.DeveloperId))
                return Forbid();

            var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
            var result = await _mediator.Send(new DeleteScriptRequest { ScriptId = id, UserId = uid });

            if (!result.Success)
                return NotFound(new { success = false, message = result.Message ?? "Script silinemedi." });

            return Json(new { success = true, message = result.Message ?? "Script silindi." });
        }
    }
}
