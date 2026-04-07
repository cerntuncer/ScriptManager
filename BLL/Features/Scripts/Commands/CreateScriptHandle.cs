using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptHandle : IRequestHandler<CreateScriptRequest, CreateScriptResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<Release> _releaseRepository;
        private readonly IRepository<DAL.Entities.User> _userRepository;
        private readonly IMediator _mediator;
        private readonly IScriptConflictSyncService _conflictSync;

        public CreateScriptHandle(
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository,
            IRepository<Release> releaseRepository,
            IRepository<DAL.Entities.User> userRepository,
            IMediator mediator,
            IScriptConflictSyncService conflictSync)
        {
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
            _releaseRepository = releaseRepository;
            _userRepository = userRepository;
            _mediator = mediator;
            _conflictSync = conflictSync;
        }

        public async Task<CreateScriptResponse> Handle(CreateScriptRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return new CreateScriptResponse { Success = false, Message = "Script adı boş olamaz." };


            if (string.IsNullOrWhiteSpace(request.SqlScript))
                return new CreateScriptResponse { Success = false, Message = "SQL Script boş olamaz." };

            var existingScript = await _scriptRepository.GetWhereAsync(x => x.Name == request.Name);
            if (existingScript.Any())
                return new CreateScriptResponse { Success = false, Message = "Bu isimde script zaten mevcut." };


            Batch? batch = null;
            if (request.BatchId.HasValue)
            {
                batch = await _batchRepository.GetByIdAsync(request.BatchId.Value);
                if (batch == null)
                    return new CreateScriptResponse { Success = false, Message = "Batch bulunamadı." };
            }
            else if (request.Batch != null)
            {
                var existingBatch = await _batchRepository.GetWhereAsync(x => x.Name == request.Batch.Name);
                if (existingBatch.Any())
                {
                    batch = existingBatch.First();
                }
                else
                {

                    var responseBatch = await _mediator.Send(request.Batch, cancellationToken);
                    if (!responseBatch.Success)
                        return new CreateScriptResponse { Success = false, Message = "Batch oluşturulamadı: " + responseBatch.Message };

                    batch = await _batchRepository.GetByIdAsync(responseBatch.BatchId);
                    if (batch == null)
                        return new CreateScriptResponse { Success = false, Message = "Batch oluşturuldu ama bulunamadı." };
                }
            }

            var user = await _userRepository.GetByIdAsync(request.DeveloperId);
            if (user == null)
                return new CreateScriptResponse { Success = false, Message = "Developer bulunamadı." };


            var script = new Script
            {
                Name = request.Name,
                SqlScript = request.SqlScript,
                RollbackScript = request.RollbackScript,
                BatchId = batch?.Id,
                DeveloperId = user.Id,
                Status = ScriptStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            await _scriptRepository.AddAsync(script);
            await _scriptRepository.SaveAsync();

            await _conflictSync.SyncAfterScriptSavedAsync(script.Id, cancellationToken);

            return new CreateScriptResponse
            {
                Success = true,
                Message = "Script başarıyla oluşturuldu.",
                ScriptId = script.Id,
                BatchId = batch?.Id
            };
        }
    }
}