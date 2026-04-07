namespace ScriptManager.Models.User;

public class SetUserActiveFormRequest
{
    public long UserId { get; set; }
    public bool IsActive { get; set; }
}
