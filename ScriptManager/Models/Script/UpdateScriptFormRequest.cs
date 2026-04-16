namespace ScriptManager.Models.Script;

public class UpdateScriptFormRequest
{
    public long ScriptId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SqlScript { get; set; } = string.Empty;
    public string? RollbackScript { get; set; }
}
