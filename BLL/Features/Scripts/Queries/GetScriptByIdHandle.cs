using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class GetScriptByIdHandle : IRequestHandler<GetScriptByIdRequest, GetScriptByIdResponse>
    {
        private readonly IRepository<Script> _scriptRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly IRepository<DAL.Entities.User> _userRepository;

        public GetScriptByIdHandle(
            IRepository<Script> scriptRepository,
            IRepository<Batch> batchRepository,
            IRepository<DAL.Entities.User> userRepository)
        {
            _scriptRepository = scriptRepository;
            _batchRepository = batchRepository;
            _userRepository = userRepository;
        }

        public async Task<GetScriptByIdResponse> Handle(GetScriptByIdRequest request, CancellationToken cancellationToken)
        {
            var script = await _scriptRepository.GetByIdAsync(request.ScriptId);

            if (script == null)
            {
                return new GetScriptByIdResponse
                {
                    Success = false,
                    Message = "Script bulunamadı."
                };
            }

            var batch = await _batchRepository.GetByIdAsync(script.BatchId);
            var user = await _userRepository.GetByIdAsync(script.DeveloperId);

            return new GetScriptByIdResponse
            {
                Success = true,
                Message = "Script getirildi.",

                ScriptId = script.Id,
                Name = script.Name,
                SqlScript = script.SqlScript,
                RollbackScript = script.RollbackScript,

                BatchId = script.BatchId,
                BatchName = batch?.Name ?? "Bilinmiyor",

                DeveloperId = script.DeveloperId,
                DeveloperName = user?.Name ?? "Bilinmiyor",

                Status = script.Status.ToString(),
                CreatedAt = script.CreatedAt
            };
        }
    }
}