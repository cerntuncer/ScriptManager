using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.User;

namespace ScriptManager.Data;

/// <summary>MVC panelinde kullanıcı listesi — EF global filtre: IsDeleted = false.</summary>
public static class UserReadQueries
{
    public static async Task<List<UserListItemViewModel>> ListForAdminPanelAsync(MyContext db)
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
            UserRole.Admin => "Yönetici",
            UserRole.Developer => "Geliştirici",
            _ => role.ToString()
        };

    private static string WorkflowHintForRole(UserRole role) =>
        role switch
        {
            UserRole.Developer =>
                "Script oluşturur (Taslak) ve Hazır durumuna alır. Hazır olmayan script release'e eklenemez.",
            UserRole.Admin =>
                "Kullanıcılar, sürümler ve tüm yönetim işlemleri. Script durumunu değiştirebilir.",
            _ => "—"
        };
}
