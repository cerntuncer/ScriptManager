namespace ScriptManager.Models.Release;

public class CreateOrphanRootFormRequest
{
    public string Name { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
}
