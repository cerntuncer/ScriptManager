using DAL.Repositories.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Features.Users.Queries
{
    public class GetScriptListHandle : IRequestHandler<GetScriptListRequest, List<GetScriptListResponse>>
    {
        private readonly IScriptRepository _scriptRepository;
        private readonly IConflictRepository _conflictRepository;

        public GetScriptListHandle(IScriptRepository scriptRepository, IConflictRepository conflictRepository)
        {
            _scriptRepository = scriptRepository;
            _conflictRepository = conflictRepository;
        }

        public async Task<List<GetScriptListResponse>> Handle(
            GetScriptListRequest request,
            CancellationToken cancellationToken)
        {
            var scripts = await _scriptRepository.GetAllDetailedAsync();
            var unresolved = await _conflictRepository.GetUnresolvedConflictsAsync();
            var keysByScript = new Dictionary<long, HashSet<string>>();
            foreach (var c in unresolved)
            {
                if (!keysByScript.TryGetValue(c.ScriptId, out var a))
                {
                    a = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    keysByScript[c.ScriptId] = a;
                }

                if (!keysByScript.TryGetValue(c.ConflictingScriptId, out var b))
                {
                    b = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    keysByScript[c.ConflictingScriptId] = b;
                }

                a.Add(c.TableName);
                b.Add(c.TableName);
            }

            return scripts.Select(x =>
            {
                var has = keysByScript.TryGetValue(x.Id, out var ts);
                return new GetScriptListResponse
                {
                    ScriptId = x.Id,
                    Name = x.Name,
                    Status = x.Status.ToString(),
                    BatchName = x.Batch?.Name ?? "Atanmamış",
                    DeveloperName = x.Developer.Name,
                    CreatedAt = x.CreatedAt,
                    HasRollback = !string.IsNullOrWhiteSpace(x.RollbackScript),
                    HasOpenConflict = has && ts!.Count > 0,
                    ConflictingTableNames = has ? ts!.OrderBy(s => s).ToList() : new List<string>()
                };
            }).ToList();
        }
    }
}
