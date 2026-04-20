using DAL.Enums;

namespace BLL.Services;

public interface IScriptConflictSyncService
{
    /// <summary>Script kaydedildikten sonra aynı release (veya yetim ağaç) içinde tablo çakışmalarını günceller ve durumları düzeltir.</summary>
    Task SyncAfterScriptSavedAsync(long scriptId, CancellationToken cancellationToken = default);

    Task<bool> HasUnresolvedConflictsAsync(long scriptId, CancellationToken cancellationToken = default);

    /// <summary>Conflict kaydı çözüldükten veya kaldırıldıktan sonra tek script için durumu tabloya göre günceller.</summary>
    Task RecomputeScriptStatusAsync(long scriptId, CancellationToken cancellationToken = default);

    /// <summary>İki scripti birlikte yeniden değerlendirir (çözüm sonrası).</summary>
    Task RecomputeScriptsAfterConflictChangeAsync(long scriptId, long otherScriptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aynı iki script arasında birden fazla açık çakışma satırı kaldıysa tek satırda birleştirir; konular konu alanında toplanır.
    /// </summary>
    Task NormalizeDuplicateOpenConflictsAsync(CancellationToken cancellationToken = default);

    /// <summary>Açık çakışma satırını siler; aynı SQL kalsın bile otomatik yeniden açılmaması için çift özeti kaydeder.</summary>
    Task RemoveOpenConflictWithDismissalAsync(long conflictId, long resolvedByUserId, ConflictResolutionKind kind,
        CancellationToken cancellationToken = default);
}
