using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Conflicts.Queries;

public class GetUnresolvedConflictsHandle : IRequestHandler<GetUnresolvedConflictsRequest, GetUnresolvedConflictsResponse>
{
    private readonly IConflictRepository _conflictRepository;

    public GetUnresolvedConflictsHandle(IConflictRepository conflictRepository)
    {
        _conflictRepository = conflictRepository;
    }

    public async Task<GetUnresolvedConflictsResponse> Handle(GetUnresolvedConflictsRequest request, CancellationToken cancellationToken)
    {
        var rows = await _conflictRepository.GetUnresolvedConflictsAsync();
        var items = rows.Select(c =>
        {
            var aName = c.Script?.Name ?? $"#{c.ScriptId}";
            var bName = c.ConflictingScript?.Name ?? $"#{c.ConflictingScriptId}";
            return new UnresolvedConflictItem
            {
                ConflictId = c.Id,
                TableName = c.TableName,
                ScriptId = c.ScriptId,
                ScriptName = aName,
                ConflictingScriptId = c.ConflictingScriptId,
                ConflictingScriptName = bName,
                DetectedAt = c.DetectedAt,
                WarningMessage =
                    $"\"{c.TableName}\" tablosu \"{aName}\" (Id:{c.ScriptId}) ve \"{bName}\" (Id:{c.ConflictingScriptId}) scriptlerinde geçiyor; çakışma olabilir, scriptleri kontrol edin."
            };
        }).ToList();

        return new GetUnresolvedConflictsResponse
        {
            Success = true,
            Message = items.Count == 0 ? "Açık çakışma kaydı yok." : $"{items.Count} açık çakışma.",
            Items = items
        };
    }
}
