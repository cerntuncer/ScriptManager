namespace ScriptManager.Models.Release;

public class AddFolderFormRequest
{
    public long ParentBatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
}
