using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Releases.Queries
{
    public class GetReleaseByIdHandle
        : IRequestHandler<GetReleaseByIdRequest, GetReleaseByIdResponse>
    {
        private readonly IReleaseRepository _releaseRepository;

        public GetReleaseByIdHandle(IReleaseRepository releaseRepository)
        {
            _releaseRepository = releaseRepository;
        }

        public async Task<GetReleaseByIdResponse> Handle(
            GetReleaseByIdRequest request,
            CancellationToken cancellationToken)
        {
            var release = await _releaseRepository.GetWithScriptsAsync(request.ReleaseId);

            if (release == null)
            {
                return new GetReleaseByIdResponse
                {
                    Success = false,
                    Message = "Release bulunamadı."
                };
            }

            var scriptDtos = new List<ReleaseScriptDto>();
            var sqlList = new List<string>();
            var rollbackList = new List<string>();
            var order = 1;

            foreach (var batch in release.Batches.Where(b => !b.IsDeleted).OrderBy(b => b.Name))
            {
                foreach (var script in batch.Scripts
                             .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted)
                             .OrderBy(s => s.Name))
                {
                    scriptDtos.Add(new ReleaseScriptDto
                    {
                        ScriptId = script.Id,
                        Name = script.Name,
                        BatchId = batch.Id,
                        BatchName = batch.Name,
                        Order = order++
                    });

                    sqlList.Add(script.SqlScript);

                    if (!string.IsNullOrWhiteSpace(script.RollbackScript))
                        rollbackList.Add(script.RollbackScript);
                }
            }

            var combinedSql = string.Join("\n\n", sqlList);
            var combinedRollback = string.Join("\n\n", rollbackList.AsEnumerable().Reverse());

            return new GetReleaseByIdResponse
            {
                Success = true,
                Message = "Release detay getirildi.",
                ReleaseId = release.Id,
                Version = release.Version,
                Scripts = scriptDtos,
                CombinedSql = combinedSql,
                CombinedRollback = combinedRollback
            };
        }
    }
}
