using DAL.Entities;
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

        public GetBatchListHandle(IRepository<Batch> batchRepository)
        {
            _batchRepository = batchRepository;
        }

        public async Task<List<GetBatchListResponse>> Handle(
            GetBatchListRequest request,
            CancellationToken cancellationToken)
        {
            var batches = await _batchRepository.GetAllAsync();

            return batches.Select(x => new GetBatchListResponse
            {
                BatchId = x.Id,
                Name = x.Name
            }).ToList();
        }
    }
}