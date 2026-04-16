namespace ScriptManager.Models.Batch;

public class PoolBatchTreeNodeDto
{
    public long BatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool CanAddScript { get; set; }
    /// <summary>Üstte script yokken alt batch eklenebilir.</summary>
    public bool CanAddChild { get; set; }
    /// <summary>Bu düğüm kökü olarak alt ağaç release için hazırsa true.</summary>
    public bool CanPackageRelease { get; set; }

    /// <summary>Aktif release ağacındaysa sürüm kimliği / etiketi (havuz sayfasında rozet).</summary>
    public long? LinkedReleaseId { get; set; }
    public string? LinkedReleaseVersion { get; set; }

    /// <summary>Kök, kilitsiz ve herhangi bir sürüme bağlı değilse silinebilir.</summary>
    public bool CanDelete { get; set; }

    public List<PoolBatchTreeNodeDto> Children { get; set; } = new();
}
