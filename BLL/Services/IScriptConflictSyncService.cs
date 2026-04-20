using DAL.Enums;

namespace BLL.Services;

public interface IScriptConflictSyncService
{
    Task SyncAfterScriptSavedAsync(long scriptId, CancellationToken cancellationToken = default);

    Task<bool> HasUnresolvedConflictsAsync(long scriptId, CancellationToken cancellationToken = default);

    Task RecomputeScriptStatusAsync(long scriptId, CancellationToken cancellationToken = default);

    Task RecomputeScriptsAfterConflictChangeAsync(long scriptId, long otherScriptId, CancellationToken cancellationToken = default);

    Task NormalizeDuplicateOpenConflictsAsync(CancellationToken cancellationToken = default);

    Task RemoveOpenConflictWithDismissalAsync(long conflictId, long resolvedByUserId, ConflictCloseReason closeReason,
        CancellationToken cancellationToken = default);
}
