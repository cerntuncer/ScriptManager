using BLL.Services;
using DAL.Context;
using DAL.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Data;
using ScriptManager.Models.Conflict;
using ScriptManager.Security;

namespace ScriptManager.Controllers;

public class ConflictsController : Controller
{
    private readonly MyContext _db;
    private readonly IScriptConflictSyncService _conflictSync;

    public ConflictsController(MyContext db, IScriptConflictSyncService conflictSync)
    {
        _db = db;
        _conflictSync = conflictSync;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Çakışmalar";
        ViewBag.CanResolveConflicts = AuthHelper.CanWriteOperational(User);
        var rows = await ConflictReadQueries.ListUnresolvedAsync(_db);
        return View(new ConflictsIndexViewModel { Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Pair(long id)
    {
        if (!AuthHelper.CanWriteOperational(User))
            return Forbid();

        var c = await _db.Conflicts
            .Include(x => x.Script).ThenInclude(s => s!.Developer)
            .Include(x => x.ConflictingScript).ThenInclude(s => s!.Developer)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (c == null || c.ResolvedAt != null)
            return NotFound(new { message = "Çakışma bulunamadı veya zaten kapatılmış." });

        if (c.Script == null || c.ConflictingScript == null)
            return BadRequest(new { message = "İlişkili script kayıtları eksik." });

        var uid = await AuthHelper.GetActorUserIdAsync(User, _db);

        bool CanEditScript(DAL.Entities.Script s) =>
            AuthHelper.IsAdmin(User) || s.DeveloperId == uid;

        object ScriptDto(DAL.Entities.Script s, bool canEdit) =>
            new
            {
                id = s.Id,
                name = s.Name,
                developer = s.Developer?.Name ?? "—",
                sqlScript = s.SqlScript ?? "",
                rollbackScript = s.RollbackScript,
                canEdit
            };

        return Json(new
        {
            conflictId = c.Id,
            tableName = c.TableName,
            scriptA = ScriptDto(c.Script, CanEditScript(c.Script)),
            scriptB = ScriptDto(c.ConflictingScript, CanEditScript(c.ConflictingScript))
        });
    }

    public class ResolveConflictForm
    {
        public long ConflictId { get; set; }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Resolve([FromBody] ResolveConflictForm? body)
    {
        if (!AuthHelper.CanWriteOperational(User))
            return Forbid();

        if (body == null || body.ConflictId <= 0)
            return BadRequest(new { success = false, message = "Geçersiz istek." });

        var row = await _db.Conflicts.FirstOrDefaultAsync(c => c.Id == body.ConflictId && !c.IsDeleted);
        if (row == null)
            return BadRequest(new { success = false, message = "Kayıt bulunamadı." });

        if (row.ResolvedAt != null)
            return BadRequest(new { success = false, message = "Bu çakışma zaten çözümlenmiş." });

        var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
        row.ResolvedBy = uid;
        row.ResolvedAt = DateTime.UtcNow;
        _db.Conflicts.Update(row);
        await _db.SaveChangesAsync();

        await _conflictSync.RecomputeScriptsAfterConflictChangeAsync(row.ScriptId, row.ConflictingScriptId);

        return Json(new { success = true, message = "Çakışma onaylandı.", conflictId = row.Id });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveReview([FromBody] SaveConflictReviewRequest? body)
    {
        if (!AuthHelper.CanWriteOperational(User))
            return Forbid();

        if (body == null || body.ConflictId <= 0)
            return BadRequest(new { success = false, message = "Geçersiz istek." });

        var conflictSnap = await _db.Conflicts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == body.ConflictId && !c.IsDeleted);

        if (conflictSnap == null || conflictSnap.ResolvedAt != null)
            return BadRequest(new { success = false, message = "Çakışma bulunamadı veya kapatılmış." });

        var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
        var allowed = new HashSet<long> { conflictSnap.ScriptId, conflictSnap.ConflictingScriptId };

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var touched = new List<long>();

            foreach (var u in body.Updates ?? new List<ScriptSqlUpdateItem>())
            {
                if (u.ScriptId <= 0 || !allowed.Contains(u.ScriptId))
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { success = false, message = "Bu çakışmaya ait olmayan script güncellenemez." });
                }

                var script = await _db.Scripts.FirstOrDefaultAsync(s =>
                    s.Id == u.ScriptId && !s.IsDeleted && s.Status != ScriptStatus.Deleted);

                if (script == null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { success = false, message = $"Script #{u.ScriptId} bulunamadı." });
                }

                var wantedSql = (u.SqlScript ?? string.Empty).Trim();
                var wantedRb = string.IsNullOrWhiteSpace(u.RollbackScript) ? null : u.RollbackScript.Trim();
                var curRb = script.RollbackScript;

                var same =
                    string.Equals(script.SqlScript?.Trim() ?? "", wantedSql, StringComparison.Ordinal) &&
                    string.Equals(curRb?.Trim() ?? "", wantedRb?.Trim() ?? "", StringComparison.Ordinal);

                if (same)
                    continue;

                if (!AuthHelper.IsAdmin(User) && script.DeveloperId != uid)
                {
                    await tx.RollbackAsync();
                    return Forbid();
                }

                if (string.IsNullOrWhiteSpace(wantedSql))
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { success = false, message = "SQL metni boş olamaz." });
                }

                script.SqlScript = wantedSql;
                script.RollbackScript = wantedRb;
                script.UpdatedAt = DateTime.UtcNow;
                _db.Scripts.Update(script);
                touched.Add(script.Id);
            }

            await _db.SaveChangesAsync();

            foreach (var sid in touched.Distinct())
                await _conflictSync.SyncAfterScriptSavedAsync(sid);

            var sidA = conflictSnap.ScriptId;
            var sidB = conflictSnap.ConflictingScriptId;

            if (body.MarkResolved)
            {
                var row = await _db.Conflicts.FirstOrDefaultAsync(c => c.Id == body.ConflictId && !c.IsDeleted);
                if (row != null && row.ResolvedAt == null)
                {
                    row.ResolvedBy = uid;
                    row.ResolvedAt = DateTime.UtcNow;
                    _db.Conflicts.Update(row);
                    await _db.SaveChangesAsync();
                }

                await _conflictSync.RecomputeScriptsAfterConflictChangeAsync(sidA, sidB);
            }

            await tx.CommitAsync();

            var msg = body.MarkResolved
                ? "Değişiklikler kaydedildi; çakışma kapatıldı."
                : touched.Count > 0
                    ? "Scriptler güncellendi. Metin artık aynı tabloda çakışmıyorsa kayıt otomatik kalkabilir; gerekirse «Çözümlendi» ile kapatın."
                    : "Kayıt güncellenmedi.";

            return Json(new { success = true, message = msg });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
