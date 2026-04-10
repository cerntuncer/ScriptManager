namespace ScriptManager.Models.Release;

public class SetReleaseTreeLockFormRequest
{
    public long ReleaseId { get; set; }
    public bool Lock { get; set; }
}
