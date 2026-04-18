using BLL.Services;

namespace ScriptManager.Models.Conflict;

public class ConflictRowViewModel
{
    public long ConflictId { get; set; }
    public string TableName { get; set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilecek okunabilir conflict etiketi.</summary>
    public string ConflictLabel => ConflictKey.ToDisplayLabel(TableName);

    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByName { get; set; }

    public string ResolvedAtDisplay => ResolvedAt.HasValue
        ? ResolvedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
        : "—";

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
    public List<ConflictRowViewModel> ResolvedRows { get; set; } = new();
}
