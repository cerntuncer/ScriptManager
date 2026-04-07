using DAL.Enums;

namespace ScriptManager.Models.User;

public class UserListItemViewModel
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public UserRole RoleEnum { get; set; }
    public bool IsActive { get; set; }
    public string WorkflowHint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
