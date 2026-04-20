namespace ScriptManager.Models.Release;

public class ReleaseBatchFolderViewModel
{
    public long BatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ReleaseBatchFolderViewModel> Folders { get; set; } = new();
    public List<ReleaseScriptItemViewModel> Scripts { get; set; } = new();
}
