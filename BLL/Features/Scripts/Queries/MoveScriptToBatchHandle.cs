using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class MoveScriptToBatchHandle
         : IRequestHandler<MoveScriptToBatchRequest, MoveScriptToBatchResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Batch> _batchRepository;

        public MoveScriptToBatchHandle(
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository)
        {
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
        }

        public async Task<MoveScriptToBatchResponse> Handle(
            MoveScriptToBatchRequest request,
            CancellationToken cancellationToken)
        {
            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);

            if (script == null)
            {
                return new MoveScriptToBatchResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            if (script.Status == ScriptStatus.Deleted)
            {
                return new MoveScriptToBatchResponse
                {
                    Success = false,
                    Message = "Silinmiş script taşınamaz."
                };
            }

            var batch = await _batchRepository.GetByIdAsync(request.NewBatchId);

            if (batch == null)
            {
                return new MoveScriptToBatchResponse
                {
                    Success = false,
                    Message = "Batch bulunamadı."
                };
            }


            if (script.BatchId == batch.Id)
            {
                return new MoveScriptToBatchResponse
                {
                    Success = false,
                    Message = "Script zaten bu batch içinde."
                };
            }

            script.BatchId = batch.Id;
            _scriptRepository.Update(script);

            await _scriptRepository.SaveAsync();

            return new MoveScriptToBatchResponse
            {
                Success = true,
                Message = "Script başarıyla taşındı.",
                ScriptId = script.Id,
                BatchId = batch.Id
            };
        }
    }
}