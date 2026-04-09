using System.Security.Claims;
using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace ScriptManager.Security;

public static class AuthHelper
{
    /// <summary>Oturum yokken (giriş sonraya bırakıldı) tüm yazma sayfalarının çalışması için kimlik doğrulanmamış kullanıcı tam yetkili sayılır.</summary>
    private static bool IsAnonymous(ClaimsPrincipal user) =>
        user?.Identity?.IsAuthenticated != true;

    public static long? GetUserId(ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(v, out var id) ? id : null;
    }

    /// <summary>İşlemi yapan kullanıcı: id claim veya aktif ilk yönetici (veya kullanıcı).</summary>
    public static async Task<long> GetActorUserIdAsync(ClaimsPrincipal user, MyContext db,
        CancellationToken cancellationToken = default)
    {
        var id = GetUserId(user);
        if (id.HasValue) return id.Value;

        var admin = await db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive && u.Role == UserRole.Admin)
            .OrderBy(u => u.Id)
            .Select(u => (long?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (admin.HasValue) return admin.Value;

        return await db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static bool IsAdmin(ClaimsPrincipal user) =>
        IsAnonymous(user) || user.IsInRole(nameof(UserRole.Admin));

    public static bool IsTester(ClaimsPrincipal user) =>
        !IsAnonymous(user) && user.IsInRole(nameof(UserRole.Tester));

    public static bool IsDeveloper(ClaimsPrincipal user) =>
        IsAnonymous(user) || user.IsInRole(nameof(UserRole.Developer));

    /// <summary>Admin veya geliştirici script oluşturma/güncelleme yapabilir.</summary>
    public static bool CanAuthorScripts(ClaimsPrincipal user) =>
        IsAdmin(user) || IsDeveloper(user);

    /// <summary>Testçi panelde çoğu yazma işlemini yapamaz.</summary>
    public static bool CanWriteOperational(ClaimsPrincipal user) =>
        IsAdmin(user) || IsDeveloper(user);

    public static bool CanDeleteScript(ClaimsPrincipal user, long scriptDeveloperId)
    {
        if (IsAdmin(user))
            return true;
        if (IsTester(user))
            return false;
        if (!IsDeveloper(user))
            return false;
        var uid = GetUserId(user);
        return uid.HasValue && uid.Value == scriptDeveloperId;
    }

    public static bool CanDeleteRelease(ClaimsPrincipal user) => IsAdmin(user);

    public static bool CanManageUsers(ClaimsPrincipal user) => IsAdmin(user);
}
