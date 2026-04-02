using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class UpdateScriptHandle : IRequestHandler<UpdateScriptRequest, UpdateScriptResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<Commit> _commitRepository;
        private readonly IMediator _mediator;

        public UpdateScriptHandle(
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository,
            IRepository<Commit> commitRepository,
            IMediator mediator)
        {
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
            _commitRepository = commitRepository;
            _mediator = mediator;
        }

        public async Task<UpdateScriptResponse> Handle(UpdateScriptRequest request, CancellationToken cancellationToken)
        {
            // 🔹 Script var mı?
            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);
            if (script == null)
            {
                return new UpdateScriptResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new UpdateScriptResponse
                {
                    Success = false,
                    Message = "Script adı boş olamaz."
                };
            }

            if (string.IsNullOrWhiteSpace(request.SqlScript))
            {
                return new UpdateScriptResponse
                {
                    Success = false,
                    Message = "SQL script boş olamaz."
                };
            }


            Batch batch = new Batch();

            if (request.BatchId.HasValue)
            {
                batch = await _batchRepository.GetByIdAsync(request.BatchId.Value);

                if (batch == null)
                {
                    return new UpdateScriptResponse
                    {
                        Success = false,
                        Message = "Batch bulunamadı."
                    };
                }
            }
            else if (request.Batch != null)
            {
                var responseBatch = await _mediator.Send(request.Batch, cancellationToken);

                if (!responseBatch.Success)
                {
                    return new UpdateScriptResponse
                    {
                        Success = false,
                        Message = "Batch oluşturulamadı: " + responseBatch.Message
                    };
                }

                batch = await _batchRepository.GetByIdAsync(responseBatch.BatchId);

                if (batch == null)
                {
                    return new UpdateScriptResponse
                    {
                        Success = false,
                        Message = "Batch oluşturuldu ama bulunamadı."
                    };
                }
            }
            else
            {
                batch = await _batchRepository.GetByIdAsync(script.BatchId);
            }


            script.Name = request.Name;
            script.SqlScript = request.SqlScript;
            script.RollbackScript = request.RollbackScript;
            script.BatchId = batch.Id;
            script.Status = (ScriptStatus)request.Status;

            _scriptRepository.Update(script);

            var commit = new Commit
            {
                ScriptId = script.Id,
                UserId = request.UserId,
                CreatedAt = DateTime.Now
            };

            await _commitRepository.AddAsync(commit);

            await _scriptRepository.SaveAsync();

            return new UpdateScriptResponse
            {
                Success = true,
                Message = "Script başarıyla güncellendi.",
                ScriptId = script.Id
            };
        }
    }
}