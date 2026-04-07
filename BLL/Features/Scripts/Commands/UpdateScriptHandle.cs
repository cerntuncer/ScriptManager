using BLL.Services;
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
        private readonly IRepository<DAL.Entities.User> _userRepository;
        private readonly IScriptWorkflowService _workflow;
        private readonly IScriptConflictSyncService _conflictSync;

        public UpdateScriptHandle(
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository,
            IRepository<Commit> commitRepository,
            IMediator mediator,
            IRepository<DAL.Entities.User> userRepository,
            IScriptWorkflowService workflow,
            IScriptConflictSyncService conflictSync)
        {
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
            _commitRepository = commitRepository;
            _mediator = mediator;
            _userRepository = userRepository;
            _workflow = workflow;
            _conflictSync = conflictSync;
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

            var actor = await _userRepository.GetByIdAsync(request.UserId);
            if (actor == null || actor.IsDeleted || !actor.IsActive)
            {
                return new UpdateScriptResponse { Success = false, Message = "Kullanıcı geçersiz." };
            }

            if (actor.Role == UserRole.Tester)
            {
                return new UpdateScriptResponse { Success = false, Message = "Testçi rolü script güncelleyemez." };
            }

            if (actor.Role == UserRole.Developer && script.DeveloperId != actor.Id)
            {
                return new UpdateScriptResponse { Success = false, Message = "Yalnızca kendi scriptinizi güncelleyebilirsiniz." };
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


            Batch? batch = null;

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
                if (script.BatchId.HasValue)
                    batch = await _batchRepository.GetByIdAsync(script.BatchId.Value);
                else
                    batch = null;
            }

            await _workflow.NormalizeStaleConflictStatusAsync(script.Id, cancellationToken);
            script = await _scriptRepository.GetByIdAsync(request.ScriptId);
            if (script == null)
            {
                return new UpdateScriptResponse { Success = false, Message = "Script bulunamadı." };
            }

            var newStatus = (ScriptStatus)request.Status;
            if (newStatus == ScriptStatus.Conflict)
            {
                return new UpdateScriptResponse
                {
                    Success = false,
                    Message = "Conflict durumu yalnızca çakışma tespitinde otomatik atanır."
                };
            }

            if (newStatus != script.Status)
            {
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new UpdateScriptResponse { Success = false, Message = "Geçersiz kullanıcı." };
                }

                var err = await _workflow.ValidateTransitionAsync(script, newStatus, user, cancellationToken);
                if (err != null)
                    return new UpdateScriptResponse { Success = false, Message = err };
            }

            script.Name = request.Name;
            script.SqlScript = request.SqlScript;
            script.RollbackScript = request.RollbackScript;
            script.BatchId = batch?.Id;
            script.Status = newStatus;

            _scriptRepository.Update(script);

            var commit = new Commit
            {
                ScriptId = script.Id,
                UserId = request.UserId,
                CreatedAt = DateTime.Now
            };

            await _commitRepository.AddAsync(commit);

            await _scriptRepository.SaveAsync();

            await _conflictSync.SyncAfterScriptSavedAsync(script.Id, cancellationToken);

            return new UpdateScriptResponse
            {
                Success = true,
                Message = "Script başarıyla güncellendi.",
                ScriptId = script.Id
            };
        }
    }
}