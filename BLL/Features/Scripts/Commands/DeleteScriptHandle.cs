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
    public class DeleteScriptHandle : IRequestHandler<DeleteScriptRequest, DeleteScriptResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Commit> _commitRepository;
        private readonly IRepository<DAL.Entities.User> _userRepository;

        public DeleteScriptHandle(
            IRepository<Script> scriptRepository,
            IRepository<Commit> commitRepository,
            IRepository<DAL.Entities.User> userRepository)
        {
            _scriptRepository = scriptRepository;
            _commitRepository = commitRepository;
            _userRepository = userRepository;
        }

        public async Task<DeleteScriptResponse> Handle(DeleteScriptRequest request, CancellationToken cancellationToken)
        {

            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);

            if (script == null)
            {
                return new DeleteScriptResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            if (script.Status == ScriptStatus.Deleted)
            {
                return new DeleteScriptResponse
                {
                    Success = false,
                    Message = "Script zaten silinmiş."
                };
            }

            var actor = await _userRepository.GetByIdAsync(request.UserId);
            if (actor == null || actor.IsDeleted || !actor.IsActive)
            {
                return new DeleteScriptResponse { Success = false, Message = "Kullanıcı geçersiz." };
            }

            if (actor.Role == UserRole.Tester)
            {
                return new DeleteScriptResponse { Success = false, Message = "Testçi rolü script silemez." };
            }

            if (actor.Role == UserRole.Developer && script.DeveloperId != actor.Id)
            {
                return new DeleteScriptResponse { Success = false, Message = "Yalnızca kendi scriptinizi silebilirsiniz." };
            }

            script.Status = ScriptStatus.Deleted;

            _scriptRepository.Update(script);


            var commit = new Commit
            {
                ScriptId = script.Id,
                UserId = request.UserId,
                CreatedAt = DateTime.Now
            };

            await _commitRepository.AddAsync(commit);

            await _scriptRepository.SaveAsync();

            return new DeleteScriptResponse
            {
                Success = true,
                Message = "Script başarıyla silindi.",
                ScriptId = script.Id
            };
        }
    }
}