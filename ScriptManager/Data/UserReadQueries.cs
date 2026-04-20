using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.User;

namespace ScriptManager.Data;

/// <summary>Kullanıcı yönetimi sayfası listesi — EF global filtre: IsDeleted = false.</summary>
public static class UserReadQueries
{
    public static async Task<List<UserListItemViewModel>> ListForUserManagementAsync(MyContext db)
    {
        var rows = await db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt })
            .ToListAsync();

        return rows.Select(u => new UserListItemViewModel
        {
            UserId = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = FormatRole(u.Role),
            RoleEnum = u.Role,
            IsActive = u.IsActive,
            WorkflowHint = WorkflowHintForRole(u.Role),
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    private static string FormatRole(UserRole role) =>
        role switch
        {
            UserRole.Developer => "Geliştirici",
            UserRole.Tester => "Testçi",
            _ => role.ToString()
        };

    private static string WorkflowHintForRole(UserRole role) =>
        role switch
        {
            UserRole.Developer =>
                "Script ve sürüm/versiyon/çakışma yönetimi. Taslağı testçiye gönderebilir veya kendi scriptinde doğrudan Hazır yapabilir.",
            UserRole.Tester =>
                "Taslak scriptleri test edip Hazır işaretler. Sürüme yalnızca tüm scriptler Hazır olunca çıkılır.",
            _ => "—"
        };
}
