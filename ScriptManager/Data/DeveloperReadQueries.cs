using DAL.Context;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.Script;

namespace ScriptManager.Data;

public static class DeveloperReadQueries
{
    public static async Task<List<UserOptionViewModel>> ListOptionsAsync(MyContext db)
    {
        var rows = await db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive &&
                        u.Role == UserRole.Developer)
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Email, u.IsActive })
            .ToListAsync();

        return rows
            .Select(u => new UserOptionViewModel
            {
                UserId = u.Id,
                Name = u.IsActive ? u.Name : $"{u.Name} (pasif)",
                Email = u.Email
            })
            .ToList();
    }
}
