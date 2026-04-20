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

    /// <summary>Oturum açık kullanıcının Id claim değeri.</summary>
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

    /// <summary>Geliştirici script oluşturma/güncelleme yapabilir.</summary>
    public static bool CanAuthorScripts(ClaimsPrincipal user) => IsDeveloper(user);

    /// <summary>Batch/sürüm yazma, çakışma çözümü vb. (testçi hariç).</summary>
    public static bool CanWriteOperational(ClaimsPrincipal user) => IsDeveloper(user);

    /// <summary>Çakışmayı kapatma / inceleme kaydı.</summary>
    public static bool CanResolveConflicts(ClaimsPrincipal user) => IsDeveloper(user);

    /// <summary>Çakışma eşleştirme JSON (okuma); testçi inceleyebilir, kayıt yine geliştirici.</summary>
    public static bool CanViewConflictPair(ClaimsPrincipal user) =>
        CanResolveConflicts(user) || IsTester(user);

    /// <summary>Taslak scripti Hazır yapma: ilgili geliştirici veya testçi.</summary>
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

    /// <summary>Taslak script içeriğini düzenleme: script sahibi geliştirici.</summary>
    public static bool CanEditDraftScriptContent(ClaimsPrincipal user, long scriptDeveloperId, ScriptStatus status)
    {
        if (status != ScriptStatus.Draft) return false;
        if (IsDeveloper(user))
        {
            var id = GetUserId(user);
            return id.HasValue && id.Value == scriptDeveloperId;
        }

        return false;
    }

    public static bool CanDeleteScript(ClaimsPrincipal user, long scriptDeveloperId)
    {
        if (!IsDeveloper(user)) return false;
        var uid = GetUserId(user);
        return uid.HasValue && uid.Value == scriptDeveloperId;
    }

    public static bool CanDeleteRelease(ClaimsPrincipal user) => IsDeveloper(user);

    public static bool CanManageUsers(ClaimsPrincipal user) => IsDeveloper(user);
}
