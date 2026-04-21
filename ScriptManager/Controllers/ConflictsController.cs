using System.Text.Json.Serialization;
using BLL.Services;
using DAL.Context;
using DAL.Entities;
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
        ViewBag.CanResolveConflicts = AuthHelper.CanResolveConflicts(User);
        ViewBag.CanViewConflictPair = AuthHelper.CanViewConflictPair(User);
        await _conflictSync.NormalizeDuplicateOpenConflictsAsync();
        var rows         = await ConflictReadQueries.ListUnresolvedAsync(_db);
        var resolvedRows = await ConflictReadQueries.ListRecentlyResolvedAsync(_db);
        return View(new ConflictsIndexViewModel { Rows = rows, ResolvedRows = resolvedRows });
    }

    [HttpGet]
    public async Task<IActionResult> CountBadge()
    {
        var count = await _db.Conflicts.AsNoTracking()
            .CountAsync(c => c.ResolvedAt == null && !c.IsDeleted);

        return PartialView("~/Views/Shared/Components/ConflictCount/Default.cshtml", count);
    }

    [HttpGet]
    public async Task<IActionResult> Pair(long id)
    {
        if (!AuthHelper.CanViewConflictPair(User))
            return Forbid();

        var c = await _db.Conflicts
            .Include(x => x.Script).ThenInclude(s => s!.Developer)
            .Include(x => x.ConflictingScript).ThenInclude(s => s!.Developer)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (c == null || c.ResolvedAt != null)
            return NotFound(new { message = "Çakışma bulunamadı veya zaten kapatılmış." });

        if (c.Script == null || c.ConflictingScript == null)
            return BadRequest(new { message = "İlişkili script kayıtları eksik." });

        var canEditScripts = AuthHelper.IsDeveloper(User);

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
            scriptA = ScriptDto(c.Script, canEditScripts),
            scriptB = ScriptDto(c.ConflictingScript, canEditScripts)
        });
    }

    public class ResolveConflictForm
    {
        public long ConflictId { get; set; }

        [JsonPropertyName("resolutionKind")]
        public int? CloseReason { get; set; }
    }

    private static ConflictCloseReason NormalizeCloseKind(int? v) =>
        v == (int)ConflictCloseReason.SqlUpdated
            ? ConflictCloseReason.SqlUpdated
            : ConflictCloseReason.NoSqlChange;
    private static async Task<Conflict?> FindOpenConflictRowAsync(
        MyContext db,
        long conflictId,
        long pairMin,
        long pairMax,
        CancellationToken cancellationToken = default)
    {
        var q = db.Conflicts.Where(c => !c.IsDeleted && c.ResolvedAt == null);

        var row = await q.FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);
        if (row != null) return row;

        row = await q.FirstOrDefaultAsync(
            c => c.ScriptId == pairMin && c.ConflictingScriptId == pairMax,
            cancellationToken);
        if (row != null) return row;

        return await q.FirstOrDefaultAsync(
            c => c.ScriptId == pairMax && c.ConflictingScriptId == pairMin,
            cancellationToken);
    }

    private static async Task<bool> PairHasOpenConflictAsync(
        MyContext db,
        long pairMin,
        long pairMax,
        CancellationToken cancellationToken = default)
    {
        var q = db.Conflicts.Where(c => !c.IsDeleted && c.ResolvedAt == null);
        return await q.AnyAsync(
                   c => c.ScriptId == pairMin && c.ConflictingScriptId == pairMax,
                   cancellationToken)
               || await q.AnyAsync(
                   c => c.ScriptId == pairMax && c.ConflictingScriptId == pairMin,
                   cancellationToken);
    }

    private async Task<(List<long> Touched, IActionResult? Error)> TryApplyReviewScriptUpdatesAsync(
        SaveConflictReviewRequest body,
        HashSet<long> allowed)
    {
        var touched = new List<long>();

        foreach (var u in body.Updates ?? new List<ScriptSqlUpdateItem>())
        {
            if (u.ScriptId <= 0 || !allowed.Contains(u.ScriptId))
                return (touched, BadRequest(new { success = false, message = "Bu çakışmaya ait olmayan script güncellenemez." }));

            var script = await _db.Scripts.FirstOrDefaultAsync(s =>
                s.Id == u.ScriptId && !s.IsDeleted && s.Status != ScriptStatus.Deleted);

            if (script == null)
                return (touched, BadRequest(new { success = false, message = $"Script #{u.ScriptId} bulunamadı." }));

            var wantedSql = (u.SqlScript ?? string.Empty).Trim();
            var wantedRb = string.IsNullOrWhiteSpace(u.RollbackScript) ? null : u.RollbackScript.Trim();
            var curRb = script.RollbackScript;

            var unchanged =
                string.Equals(script.SqlScript?.Trim() ?? "", wantedSql, StringComparison.Ordinal) &&
                string.Equals(curRb?.Trim() ?? "", wantedRb?.Trim() ?? "", StringComparison.Ordinal);

            if (unchanged)
                continue;

            if (string.IsNullOrWhiteSpace(wantedSql))
                return (touched, BadRequest(new { success = false, message = "SQL metni boş olamaz." }));

            script.SqlScript = wantedSql;
            script.RollbackScript = wantedRb;
            script.UpdatedAt = DateTime.UtcNow;
            _db.Scripts.Update(script);
            touched.Add(script.Id);
        }

        return (touched, null);
    }

    private static ConflictCloseReason ResolveSaveReviewCloseReason(
        SaveConflictReviewRequest body,
        IReadOnlyList<long> touched)
    {
        if (touched.Count > 0)
            return ConflictCloseReason.SqlUpdated;
        if (body.CloseReason.HasValue &&
            Enum.IsDefined(typeof(ConflictCloseReason), body.CloseReason.Value))
            return (ConflictCloseReason)body.CloseReason.Value;
        return ConflictCloseReason.NoSqlChange;
    }

    private async Task<object?> BuildRecentResolvedPayloadAsync(long scriptId, long otherScriptId,
        ConflictCloseReason reason, long resolvedByUserId)
    {
        var s1 = await _db.Scripts.AsNoTracking()
            .Include(s => s.Developer)
            .FirstOrDefaultAsync(s => s.Id == scriptId && !s.IsDeleted);
        var s2 = await _db.Scripts.AsNoTracking()
            .Include(s => s.Developer)
            .FirstOrDefaultAsync(s => s.Id == otherScriptId && !s.IsDeleted);
        var actor = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == resolvedByUserId && !u.IsDeleted);
        if (s1 == null || s2 == null) return null;

        var at = DateTime.UtcNow;
        return new
        {
            kindDisplay = ConflictRowViewModel.FormatCloseReasonDisplay(reason),
            scriptId = s1.Id,
            scriptName = s1.Name,
            scriptDeveloper = s1.Developer?.Name ?? "—",
            otherScriptId = s2.Id,
            otherScriptName = s2.Name,
            otherDeveloper = s2.Developer?.Name ?? "—",
            resolvedByName = actor?.Name ?? "—",
            resolvedAtDisplay = at.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
        };
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Resolve([FromBody] ResolveConflictForm? body)
    {
        if (!AuthHelper.CanResolveConflicts(User))
            return Forbid();

        if (body == null || body.ConflictId <= 0)
            return BadRequest(new { success = false, message = "Geçersiz istek." });

        var row = await _db.Conflicts.FirstOrDefaultAsync(c => c.Id == body.ConflictId && !c.IsDeleted);
        if (row == null)
            return BadRequest(new { success = false, message = "Kayıt bulunamadı." });

        if (row.ResolvedAt != null)
            return BadRequest(new { success = false, message = "Bu çakışma zaten çözümlenmiş." });

        var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
        var sidA = row.ScriptId;
        var sidB = row.ConflictingScriptId;
        var closeReason = NormalizeCloseKind(body.CloseReason);
        await _conflictSync.RemoveOpenConflictWithDismissalAsync(row.Id, uid, closeReason);
        await _conflictSync.RecomputeScriptsAfterConflictChangeAsync(sidA, sidB);

        var recentResolved = await BuildRecentResolvedPayloadAsync(sidA, sidB, closeReason, uid);

        return Json(new
        {
            success = true,
            message = "Çakışma kaydı kapatıldı.",
            conflictId = body.ConflictId,
            recentResolved
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveReview([FromBody] SaveConflictReviewRequest? body)
    {
        if (!AuthHelper.CanResolveConflicts(User))
            return Forbid();

        if (body == null || body.ConflictId <= 0)
            return BadRequest(new { success = false, message = "Geçersiz istek." });

        var conflictSnap = await _db.Conflicts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == body.ConflictId && !c.IsDeleted);

        if (conflictSnap == null || conflictSnap.ResolvedAt != null)
            return BadRequest(new { success = false, message = "Çakışma bulunamadı veya kapatılmış." });

        var uid = await AuthHelper.GetActorUserIdAsync(User, _db);
        var allowed = new HashSet<long> { conflictSnap.ScriptId, conflictSnap.ConflictingScriptId };
        var pairMin = Math.Min(conflictSnap.ScriptId, conflictSnap.ConflictingScriptId);
        var pairMax = Math.Max(conflictSnap.ScriptId, conflictSnap.ConflictingScriptId);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var (touched, updateError) = await TryApplyReviewScriptUpdatesAsync(body, allowed);
            if (updateError != null)
            {
                await tx.RollbackAsync();
                return updateError;
            }

            await _db.SaveChangesAsync();

            foreach (var sid in touched.Distinct())
                await _conflictSync.SyncAfterScriptSavedAsync(sid);

            var sidA = conflictSnap.ScriptId;
            var sidB = conflictSnap.ConflictingScriptId;

            object? recentResolved = null;
            if (body.MarkResolved)
            {
                var closeReason = ResolveSaveReviewCloseReason(body, touched);
                var rowToClose = await FindOpenConflictRowAsync(_db, body.ConflictId, pairMin, pairMax);

                if (rowToClose != null)
                    await _conflictSync.RemoveOpenConflictWithDismissalAsync(rowToClose.Id, uid, closeReason);

                await _conflictSync.RecomputeScriptsAfterConflictChangeAsync(sidA, sidB);

                if (rowToClose != null)
                    recentResolved = await BuildRecentResolvedPayloadAsync(sidA, sidB, closeReason, uid);
            }

            var stillOpen = await PairHasOpenConflictAsync(_db, pairMin, pairMax);

            // Çakışma sync tarafından otomatik kaldırıldıysa çözümlendi olarak işaretle
            if (!body.MarkResolved && !stillOpen && touched.Count > 0)
            {
                var autoRecent = await BuildRecentResolvedPayloadAsync(
                    conflictSnap.ScriptId, conflictSnap.ConflictingScriptId,
                    ConflictCloseReason.SqlUpdated, uid);
                await tx.CommitAsync();
                return Json(new
                {
                    success = true,
                    autoResolved = true,
                    message = "Scriptler güncellendi; çakışma otomatik olarak çözümlendi.",
                    conflictId = body.ConflictId,
                    recentResolved = autoRecent
                });
            }

            await tx.CommitAsync();

            var msg = body.MarkResolved
                ? "Değişiklikler kaydedildi; çakışma kapatıldı."
                : touched.Count > 0
                    ? "Scriptler güncellendi."
                    : "Kayıt güncellenmedi.";

            return Json(new
            {
                success = true,
                autoResolved = body.MarkResolved,
                message = msg,
                conflictId = body.ConflictId,
                recentResolved
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
