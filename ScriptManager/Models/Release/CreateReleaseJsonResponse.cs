namespace ScriptManager.Models.Release;

public class CreateReleaseJsonResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ReleaseId { get; set; }
    public string Version { get; set; } = string.Empty;
    public int ScriptCount { get; set; }
    public int RollbackScriptCount { get; set; }
    public string CreatedAtDisplay { get; set; } = string.Empty;
    public long? RootBatchId { get; set; }
}
