using BLL.Features.Scripts.Commands;
using BLL.Services;
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
        private readonly ISqlScriptSyntaxValidator _sqlSyntax;
        private readonly IScriptConflictSyncService _conflictSync;

        public ScriptsController(MyContext db, IMediator mediator, ISqlScriptSyntaxValidator sqlSyntax, IScriptConflictSyncService conflictSync)
        {
            _db = db;
            _mediator = mediator;
            _sqlSyntax = sqlSyntax;
            _conflictSync = conflictSync;
        }

        /// <summary>SQL Server NOEXEC + ScriptDom + heuristik ile T-SQL kontrolü (kaydetmeden).</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult ValidateSql([FromBody] SqlSyntaxValidationRequest? body)
        {
            if (body == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var issues = new List<SqlScriptSyntaxIssue>();
            issues.AddRange(_sqlSyntax.Validate(body.SqlScript, "SQL").Issues);
            issues.AddRange(_sqlSyntax.Validate(body.RollbackScript, "Rollback").Issues);

            return Json(new
            {
                success = true,
                isValid = issues.Count == 0,
                issues = issues.Select(i => new
                {
                    source = i.Source,
                    batchNumber = i.BatchNumber,
                    line = i.Line,
                    column = i.Column,
                    message = i.Message
                })
            });
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Scriptler";
            ViewBag.CanAuthorScripts = AuthHelper.CanAuthorScripts(User);
            ViewBag.IsAdmin = AuthHelper.IsAdmin(User);
            ViewBag.ActorUserId = AuthHelper.GetUserId(User) ?? 0L;
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

        /// <summary>Topbar global arama — script adı, developer, versiyon üzerinden arar.</summary>
        [HttpGet]
        public async Task<IActionResult> QuickSearch([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new { results = Array.Empty<object>() });

            var term = q.Trim().ToLower();

            var scripts = await _db.Scripts.AsNoTracking()
                .Include(s => s.Developer)
                .Include(s => s.Batch)
                .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted
                    && (s.Name.ToLower().Contains(term)
                        || (s.Developer != null && s.Developer.Name.ToLower().Contains(term))
                        || (s.Batch != null && s.Batch.Name.ToLower().Contains(term))))
                .OrderByDescending(s => s.CreatedAt)
                .Take(7)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    developer = s.Developer != null ? s.Developer.Name : "",
                    batch = s.Batch != null ? s.Batch.Name : "",
                    status = s.Status.ToString()
                })
                .ToListAsync();

            return Json(new { results = scripts });
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
                (actor.Role == UserRole.Admin || actor.Role == UserRole.Developer);

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
                StatusKey = ScriptStatus.Draft.ToString(),
                BatchName = batchDisplay,
                DeveloperName = result.DeveloperName ?? string.Empty,
                HasRollback = !string.IsNullOrWhiteSpace(medReq.RollbackScript),
                CreatedAtDisplay = DateTime.UtcNow.ToLocalTime().ToString("dd.MM.yyyy"),
                CanDelete = AuthHelper.CanDeleteScript(User, developerId)
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Update([FromBody] UpdateScriptFormRequest? body)
        {
            if (body == null || body.ScriptId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.SqlScript))
                return BadRequest(new { success = false, message = "Ad ve SQL zorunludur." });

            var script = await _db.Scripts.FirstOrDefaultAsync(s => s.Id == body.ScriptId && !s.IsDeleted);
            if (script == null)
                return NotFound(new { success = false, message = "Script bulunamadı." });

            if (script.Status != ScriptStatus.Draft)
                return BadRequest(new { success = false, message = "Sadece Taslak durumdaki scriptler düzenlenebilir." });

            if (!AuthHelper.CanDeleteScript(User, script.DeveloperId))
                return StatusCode(403, new { success = false, message = "Bu scripti düzenleme yetkiniz yok." });

            script.Name = body.Name.Trim();
            script.SqlScript = body.SqlScript;
            script.RollbackScript = string.IsNullOrWhiteSpace(body.RollbackScript) ? null : body.RollbackScript;

            await _db.SaveChangesAsync();
            await _conflictSync.SyncAfterScriptSavedAsync(script.Id);

            return Json(new { success = true, message = "Script güncellendi." });
        }

        /// <summary>
        /// Conflict tespiti için tanılama: bir script'in ürettiği keyleri,
        /// bulunan peer'ları ve neden çakışıp çakışmadığını döner.
        /// Sadece geliştirme/test amacıyla kullanılır.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ConflictDiagnose(long id)
        {
            var script = await _db.Scripts
                .Include(s => s.Batch)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (script == null)
                return NotFound(new { message = "Script bulunamadı." });

            var myKeys = BLL.Services.SqlConflictKeyExtractor.ExtractFromScript(script.SqlScript, script.RollbackScript);

            var peers = await _db.Scripts
                .Include(s => s.Batch)
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted && s.Id != id)
                .ToListAsync();

            var peerDiag = new List<object>();
            foreach (var peer in peers)
            {
                var peerKeys = BLL.Services.SqlConflictKeyExtractor.ExtractFromScript(peer.SqlScript, peer.RollbackScript);
                var hits = new List<string>();
                foreach (var mk in myKeys)
                foreach (var pk in peerKeys)
                    if (BLL.Services.ConflictKey.DoConflict(mk, pk))
                        hits.Add($"{mk.Serialize()} × {pk.Serialize()} → {BLL.Services.ConflictKey.CanonicalKey(mk, pk)}");

                peerDiag.Add(new
                {
                    peerId       = peer.Id,
                    peerName     = peer.Name,
                    peerBatch    = peer.Batch?.Name ?? "(atanmamış)",
                    peerBatchId  = peer.BatchId,
                    peerReleaseId = peer.Batch?.ReleaseId,
                    peerKeys     = peerKeys.Select(k => k.Serialize()).ToList(),
                    conflictHits = hits,
                    wouldConflict = hits.Count > 0
                });
            }

            return Json(new
            {
                scriptId     = script.Id,
                scriptName   = script.Name,
                batchId      = script.BatchId,
                batchName    = script.Batch?.Name ?? "(atanmamış)",
                releaseId    = script.Batch?.ReleaseId,
                myKeys       = myKeys.Select(k => k.Serialize()).ToList(),
                peers        = peerDiag
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
