using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Queries
{
    public class GetBatchListHandle
      : IRequestHandler<GetBatchListRequest, List<GetBatchListResponse>>
    {
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<Script> _scriptRepository;

        public GetBatchListHandle(IRepository<Batch> batchRepository, IRepository<Script> scriptRepository)
        {
            _batchRepository = batchRepository;
            _scriptRepository = scriptRepository;
        }

        public async Task<List<GetBatchListResponse>> Handle(
            GetBatchListRequest request,
            CancellationToken cancellationToken)
        {
            var batches = (await _batchRepository.GetAllAsync()).OrderByDescending(x => x.CreatedAt).ToList();
            var batchIds = batches.Select(b => b.Id).ToList();
            var scripts = batchIds.Count == 0
                ? new List<Script>()
                : await _scriptRepository.GetWhereAsync(s =>
                    s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value) && s.Status != ScriptStatus.Deleted);

            var countByBatch = scripts
                .Where(s => s.BatchId.HasValue)
                .GroupBy(s => s.BatchId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return batches.Select(x => new GetBatchListResponse
            {
                BatchId = x.Id,
                Name = x.Name,
                ScriptCount = countByBatch.GetValueOrDefault(x.Id)
            }).ToList();
        }
    }
}