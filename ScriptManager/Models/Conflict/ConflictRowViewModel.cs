namespace ScriptManager.Models.Conflict;

public class ConflictRowViewModel
{
    public long ConflictId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }

    public long ScriptId { get; set; }
    public string ScriptName { get; set; } = string.Empty;
    public string ScriptDeveloper { get; set; } = string.Empty;

    public long OtherScriptId { get; set; }
    public string OtherScriptName { get; set; } = string.Empty;
    public string OtherDeveloper { get; set; } = string.Empty;
}

public class ConflictsIndexViewModel
{
    public List<ConflictRowViewModel> Rows { get; set; } = new();
}
