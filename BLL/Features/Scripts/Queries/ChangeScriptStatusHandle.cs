using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class ChangeScriptStatusHandle
         : IRequestHandler<ChangeScriptStatusRequest, ChangeScriptStatusResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Commit> _commitRepository;
        private readonly IRepository<DAL.Entities.User> _userRepository;
        private readonly IScriptWorkflowService _workflow;

        public ChangeScriptStatusHandle(
            IRepository<Script> scriptRepository,
            IRepository<Commit> commitRepository,
            IRepository<DAL.Entities.User> userRepository,
            IScriptWorkflowService workflow)
        {
            _scriptRepository = scriptRepository;
            _commitRepository = commitRepository;
            _userRepository = userRepository;
            _workflow = workflow;
        }

        public async Task<ChangeScriptStatusResponse> Handle(
            ChangeScriptStatusRequest request,
            CancellationToken cancellationToken)
        {
            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);

            if (script == null)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            await _workflow.NormalizeStaleConflictStatusAsync(script.Id, cancellationToken);
            script = await _scriptRepository.GetByIdAsync(request.ScriptId);
            if (script == null)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            if (script.Status == ScriptStatus.Deleted)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = "Silinmiş scriptin durumu değiştirilemez."
                };
            }


            if (!Enum.IsDefined(typeof(ScriptStatus), request.NewStatus))
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = $"Geçersiz status değeri: {request.NewStatus}"
                };
            }

            var newStatus = (ScriptStatus)request.NewStatus;

            if (newStatus == ScriptStatus.Deleted)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = "Silme işlemi için DeleteScript kullanılmalıdır."
                };
            }

            if (script.Status == newStatus)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = "Script zaten bu durumda."
                };
            }


            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı."
                };
            }

            var validation = await _workflow.ValidateTransitionAsync(script, newStatus, user, cancellationToken);
            if (validation != null)
            {
                return new ChangeScriptStatusResponse
                {
                    Success = false,
                    Message = validation
                };
            }

            var prev = script.Status;
            script.Status = newStatus;
            _scriptRepository.Update(script);

            var commit = new Commit
            {
                ScriptId = script.Id,
                UserId = user.Id,
                Type = CommitType.StatusChange,
                Description = $"{prev} → {newStatus}",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _commitRepository.AddAsync(commit);


            await _scriptRepository.SaveAsync();

            return new ChangeScriptStatusResponse
            {
                Success = true,
                Message = "Script durumu güncellendi.",
                ScriptId = script.Id,
                Status = newStatus.ToString()
            };
        }
    }
}