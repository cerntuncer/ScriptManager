namespace ScriptManager.Models.Batch;

public class PoolBatchTreeNodeDto
{
    public long BatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool CanAddScript { get; set; }
    public bool CanAddChild { get; set; }
    public bool CanPackageRelease { get; set; }

    public long? LinkedReleaseId { get; set; }
    public string? LinkedReleaseVersion { get; set; }

    public bool CanDelete { get; set; }

    public List<PoolBatchTreeNodeDto> Children { get; set; } = new();
}
