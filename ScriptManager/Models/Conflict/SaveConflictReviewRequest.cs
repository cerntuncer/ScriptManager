using System.Text.Json.Serialization;

namespace ScriptManager.Models.Conflict;

public class SaveConflictReviewRequest
{
    [JsonPropertyName("conflictId")]
    public long ConflictId { get; set; }

    [JsonPropertyName("updates")]
    public List<ScriptSqlUpdateItem>? Updates { get; set; }

    [JsonPropertyName("markResolved")]
    public bool MarkResolved { get; set; }

    [JsonPropertyName("resolutionKind")]
    public int? CloseReason { get; set; }
}

public class ScriptSqlUpdateItem
{
    [JsonPropertyName("scriptId")]
    public long ScriptId { get; set; }

    [JsonPropertyName("sqlScript")]
    public string SqlScript { get; set; } = string.Empty;

    [JsonPropertyName("rollbackScript")]
    public string? RollbackScript { get; set; }
}
