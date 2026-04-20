namespace DAL.Enums;

/// <summary>Çakışma kaydı kapatılırken nasıl çözümlendiği.</summary>
public enum ConflictResolutionKind
{
    Unspecified = 0,
    /// <summary>SQL/içerik düzeltildi (veya düzeltildi beyanı).</summary>
    FixedWithSqlChange = 1,
    /// <summary>Düzeltme yapılmadan kayıt kapatıldı.</summary>
    ClosedWithoutSqlChange = 2
}
