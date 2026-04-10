namespace ScriptManager.Models.Release;

public class CreateReleaseFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public string RootMode { get; set; } = "new";
    public string? NewRootBatchName { get; set; }
    public long? ExistingRootBatchId { get; set; }
}
