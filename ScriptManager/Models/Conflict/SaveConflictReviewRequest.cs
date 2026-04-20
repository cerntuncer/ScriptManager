using DAL.Enums;

namespace ScriptManager.Models.Conflict;

public class SaveConflictReviewRequest
{
    public long ConflictId { get; set; }
    public List<ScriptSqlUpdateItem>? Updates { get; set; }

    public bool MarkResolved { get; set; }

    public int? ResolutionKind { get; set; }
}

public class ScriptSqlUpdateItem
{
    public long ScriptId { get; set; }
    public string SqlScript { get; set; } = string.Empty;
    public string? RollbackScript { get; set; }
}
