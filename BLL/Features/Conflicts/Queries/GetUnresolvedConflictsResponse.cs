using BLL.Common;

namespace BLL.Features.Conflicts.Queries;

public class GetUnresolvedConflictsResponse : BaseResponse
{
    public List<UnresolvedConflictItem> Items { get; set; } = new();
}

public class UnresolvedConflictItem
{
    public long ConflictId { get; set; }
    public string TableName { get; set; } = null!;
    public long ScriptId { get; set; }
    public string ScriptName { get; set; } = null!;
    public long ConflictingScriptId { get; set; }
    public string ConflictingScriptName { get; set; } = null!;
    public DateTime DetectedAt { get; set; }
    /// <summary>UI uyarı metni: aynı tabloya dokunan iki script; kontrol edin.</summary>
    public string WarningMessage { get; set; } = null!;
}
