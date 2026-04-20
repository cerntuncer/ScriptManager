using System.Security.Claims;
using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace ScriptManager.Security;

public static class AuthHelper
{
    public static long? GetUserId(ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(v, out var id) ? id : null;
    }

    public static async Task<long> GetActorUserIdAsync(ClaimsPrincipal user, MyContext db,
        CancellationToken cancellationToken = default)
    {
        var id = GetUserId(user);
        if (id.HasValue) return id.Value;

        await Task.CompletedTask;
        throw new InvalidOperationException("Oturum kimliği bulunamadı; yeniden giriş yapın.");
    }

    public static bool IsDeveloper(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.IsInRole(nameof(UserRole.Developer));

    public static bool IsTester(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.IsInRole(nameof(UserRole.Tester));

    public static bool CanAuthorScripts(ClaimsPrincipal user) => IsDeveloper(user);

    public static bool CanWriteOperational(ClaimsPrincipal user) => IsDeveloper(user);

    public static bool CanResolveConflicts(ClaimsPrincipal user) => IsDeveloper(user);

    public static bool CanViewConflictPair(ClaimsPrincipal user) =>
        CanResolveConflicts(user) || IsTester(user);

    public static bool CanMarkDraftScriptReady(ClaimsPrincipal user, long scriptDeveloperId, ScriptStatus status)
    {
        if (status != ScriptStatus.Draft) return false;
        if (IsTester(user)) return true;
        if (IsDeveloper(user))
        {
            var id = GetUserId(user);
            return id.HasValue && id.Value == scriptDeveloperId;
        }

        return false;
    }

    public static bool CanEditDraftScriptContent(ClaimsPrincipal user, long scriptDeveloperId, ScriptStatus status)
    {
        if (status != ScriptStatus.Draft && status != ScriptStatus.PendingTesterReview && status != ScriptStatus.Conflict)
            return false;
        if (IsDeveloper(user))
        {
            var id = GetUserId(user);
            return id.HasValue && id.Value == scriptDeveloperId;
        }

        return false;
    }

    public static bool CanSendDraftToTester(ClaimsPrincipal user, long scriptDeveloperId, ScriptStatus status)
    {
        if (status != ScriptStatus.Draft) return false;
        if (!IsDeveloper(user)) return false;
        var id = GetUserId(user);
        return id.HasValue && id.Value == scriptDeveloperId;
    }

    public static bool CanApprovePendingTesterReview(ClaimsPrincipal user, ScriptStatus status) =>
        IsTester(user) && status == ScriptStatus.PendingTesterReview;

    public static bool CanDeleteScript(ClaimsPrincipal user, long scriptDeveloperId)
    {
        if (!IsDeveloper(user)) return false;
        var uid = GetUserId(user);
        return uid.HasValue && uid.Value == scriptDeveloperId;
    }

    public static bool CanDeleteRelease(ClaimsPrincipal user) => IsDeveloper(user);
}
