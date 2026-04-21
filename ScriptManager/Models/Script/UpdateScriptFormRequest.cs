using System.Text.Json.Serialization;

namespace ScriptManager.Models.Script;

public class UpdateScriptFormRequest
{
    [JsonPropertyName("scriptId")]
    public long ScriptId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sqlScript")]
    public string SqlScript { get; set; } = string.Empty;

    [JsonPropertyName("rollbackScript")]
    public string? RollbackScript { get; set; }
}
