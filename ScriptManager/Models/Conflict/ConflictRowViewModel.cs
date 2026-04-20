using BLL.Services;
using DAL.Enums;

namespace ScriptManager.Models.Conflict;

public class ConflictRowViewModel
{
    public long ConflictId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public ConflictSeverity Severity { get; set; }

    public string ConflictLabel => string.IsNullOrWhiteSpace(TableName)
        ? "Çözümlenen çift"
        : ConflictKey.ToDisplayLabel(TableName);

    public bool IsReviewAdvised => Severity == ConflictSeverity.ReviewAdvised;

    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByName { get; set; }

    public ConflictResolutionKind? ResolutionKind { get; set; }

    public string ResolvedKindDisplay => FormatResolvedKindDisplay(ResolutionKind);

    public static string FormatResolvedKindDisplay(ConflictResolutionKind? kind) => kind switch
    {
        ConflictResolutionKind.FixedWithSqlChange => "Kapatıldı (SQL güncellendi)",
        ConflictResolutionKind.ClosedWithoutSqlChange => "Kapatıldı (SQL’e dokunulmadı)",
        _ => "Kapatıldı"
    };

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
