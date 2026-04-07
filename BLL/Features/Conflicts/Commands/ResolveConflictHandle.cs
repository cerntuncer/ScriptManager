using BLL.Services;
using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Conflicts.Commands;

public class ResolveConflictHandle : IRequestHandler<ResolveConflictRequest, ResolveConflictResponse>
{
    private readonly IRepository<Conflict> _conflictRepository;
    private readonly IRepository<DAL.Entities.User> _userRepository;
    private readonly IScriptConflictSyncService _conflictSync;

    public ResolveConflictHandle(
        IRepository<Conflict> conflictRepository,
        IRepository<DAL.Entities.User> userRepository,
        IScriptConflictSyncService conflictSync)
    {
        _conflictRepository = conflictRepository;
        _userRepository = userRepository;
        _conflictSync = conflictSync;
    }

    public async Task<ResolveConflictResponse> Handle(ResolveConflictRequest request, CancellationToken cancellationToken)
    {
        var row = await _conflictRepository.GetByIdAsync(request.ConflictId);
        if (row == null)
            return new ResolveConflictResponse { Success = false, Message = "Çakışma kaydı bulunamadı." };

        if (row.ResolvedAt != null)
            return new ResolveConflictResponse { Success = false, Message = "Bu çakışma zaten çözümlenmiş." };

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return new ResolveConflictResponse { Success = false, Message = "Geçersiz kullanıcı." };

        row.ResolvedBy = user.Id;
        row.ResolvedAt = DateTime.UtcNow;
        _conflictRepository.Update(row);
        await _conflictRepository.SaveAsync();

        await _conflictSync.RecomputeScriptsAfterConflictChangeAsync(row.ScriptId, row.ConflictingScriptId, cancellationToken);

        return new ResolveConflictResponse
        {
            Success = true,
            Message = "Çakışma onaylandı; script durumları güncellendi.",
            ConflictId = row.Id
        };
    }
}
