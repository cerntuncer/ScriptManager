using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Releases.Queries
{
    public class GetReleasesHandle
        : IRequestHandler<GetReleasesRequest, List<GetReleaseListItemResponse>>
    {
        private readonly IRepository<Release> _releaseRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<Script> _scriptRepository;

        public GetReleasesHandle(
            IRepository<Release> releaseRepository,
            IRepository<Batch> batchRepository,
            IRepository<Script> scriptRepository)
        {
            _releaseRepository = releaseRepository;
            _batchRepository = batchRepository;
            _scriptRepository = scriptRepository;
        }

        public async Task<List<GetReleaseListItemResponse>> Handle(
            GetReleasesRequest request,
            CancellationToken cancellationToken)
        {
            var releases = (await _releaseRepository.GetWhereAsync(x => !x.IsDeleted))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            if (releases.Count == 0)
                return new List<GetReleaseListItemResponse>();

            var releaseIds = releases.Select(r => r.Id).ToList();
            var batches = (await _batchRepository.GetWhereAsync(b =>
                    b.ReleaseId.HasValue && releaseIds.Contains(b.ReleaseId.Value) && !b.IsDeleted))
                .ToList();

            var batchIds = batches.Select(b => b.Id).ToList();
            var scripts = batchIds.Count == 0
                ? new List<Script>()
                : await _scriptRepository.GetWhereAsync(s =>
                    s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value) && !s.IsDeleted && s.Status != ScriptStatus.Deleted);

            var scriptsByBatch = scripts
                .GroupBy(s => s.BatchId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var batchIdsByRelease = batches
                .Where(b => b.ReleaseId.HasValue)
                .GroupBy(b => b.ReleaseId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var result = new List<GetReleaseListItemResponse>();
            foreach (var x in releases)
            {
                var bIds = batchIdsByRelease.GetValueOrDefault(x.Id) ?? new List<long>();
                var relScripts = bIds.SelectMany(bid => scriptsByBatch.GetValueOrDefault(bid) ?? new List<Script>()).ToList();
                var rb = relScripts.Count(s => !string.IsNullOrWhiteSpace(s.RollbackScript));

                result.Add(new GetReleaseListItemResponse
                {
                    ReleaseId = x.Id,
                    Version = x.Version,
                    CreatedAt = x.CreatedAt,
                    ScriptCount = relScripts.Count,
                    RollbackScriptCount = rb
                });
            }

            return result;
        }
    }
}
