using DAL.Common;
using DAL.Enums;

namespace DAL.Entities;

/// <summary>
/// İki script çifti için çakışmanın kullanıcı tarafından kapatıldığını ve mevcut SQL özetleri
/// değişmedikçe otomatik çakışma kaydı oluşturulmaması gerektiğini tutar.
/// </summary>
public class ConflictPairDismissal : BaseEntity
{
    public long ScriptIdMin { get; set; }
    public long ScriptIdMax { get; set; }
    public string SqlFingerprintMin { get; set; } = null!;
    public string SqlFingerprintMax { get; set; } = null!;
    public long? ResolvedByUserId { get; set; }
    public ConflictResolutionKind? ResolutionKind { get; set; }

    public User? ResolvedByUser { get; set; }
    public Script? ScriptMin { get; set; }
    public Script? ScriptMax { get; set; }
}
