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
    public class GetBatchByIdHandle
          : IRequestHandler<GetBatchByIdRequest, GetBatchByIdResponse>
    {
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<Script> _scriptRepository;

        public GetBatchByIdHandle(
            IRepository<Batch> batchRepository,
            IRepository<Script> scriptRepository)
        {
            _batchRepository = batchRepository;
            _scriptRepository = scriptRepository;
        }

        public async Task<GetBatchByIdResponse> Handle(
            GetBatchByIdRequest request,
            CancellationToken cancellationToken)
        {
            var batch = await _batchRepository.GetByIdAsync(request.BatchId);

            if (batch == null)
            {
                return new GetBatchByIdResponse
                {
                    Success = false,
                    Message = "Batch bulunamadı."
                };
            }

            // 🔥 Batch içindeki scriptler
            var scripts = await _scriptRepository
                .GetWhereAsync(x => x.BatchId == batch.Id && x.Status != ScriptStatus.Deleted);

            var scriptDtos = scripts.Select(x => new BatchScriptDto
            {
                ScriptId = x.Id,
                Name = x.Name,
                Status = x.Status.ToString()
            }).ToList();

            return new GetBatchByIdResponse
            {
                Success = true,
                Message = "Batch detay getirildi.",

                BatchId = batch.Id,
                Name = batch.Name,
                Scripts = scriptDtos
            };
        }
    }
}