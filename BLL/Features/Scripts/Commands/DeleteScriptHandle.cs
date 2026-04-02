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

        public DeleteScriptHandle(
            IRepository<Script> scriptRepository,
            IRepository<Commit> commitRepository)
        {
            _scriptRepository = scriptRepository;
            _commitRepository = commitRepository;
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