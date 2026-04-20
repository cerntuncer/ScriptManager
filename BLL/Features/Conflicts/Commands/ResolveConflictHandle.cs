using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Conflicts.Commands;

public class ResolveConflictHandle : IRequestHandler<ResolveConflictRequest, ResolveConflictResponse>
{
    private readonly IRepository<Conflict> _conflictRepository;
    private readonly IScriptConflictSyncService _conflictSync;

    public ResolveConflictHandle(
        IRepository<Conflict> conflictRepository,
        IScriptConflictSyncService conflictSync)
    {
        _conflictRepository = conflictRepository;
        _conflictSync = conflictSync;
    }

    public async Task<ResolveConflictResponse> Handle(ResolveConflictRequest request, CancellationToken cancellationToken)
    {
        var row = await _conflictRepository.GetByIdAsync(request.ConflictId);
        if (row == null)
            return new ResolveConflictResponse { Success = false, Message = "Çakışma kaydı bulunamadı." };

        if (row.ResolvedAt != null)
            return new ResolveConflictResponse { Success = false, Message = "Bu çakışma zaten çözümlenmiş." };

        var sidA = row.ScriptId;
        var sidB = row.ConflictingScriptId;
        await _conflictSync.RemoveOpenConflictWithDismissalAsync(
            request.ConflictId,
            request.UserId,
            ConflictCloseReason.NoSqlChange,
            cancellationToken);

        await _conflictSync.RecomputeScriptsAfterConflictChangeAsync(sidA, sidB, cancellationToken);

        return new ResolveConflictResponse
        {
            Success = true,
            Message = "Çakışma onaylandı; script durumları güncellendi.",
            ConflictId = request.ConflictId
        };
    }
}
