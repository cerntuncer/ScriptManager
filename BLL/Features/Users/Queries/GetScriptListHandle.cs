using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetScriptListHandle : IRequestHandler<GetScriptListRequest, List<GetScriptListResponse>>
    {
        private readonly IScriptRepository _scriptRepository;

        public GetScriptListHandle(IScriptRepository scriptRepository)
        {
            _scriptRepository = scriptRepository;
        }

        public async Task<List<GetScriptListResponse>> Handle(
            GetScriptListRequest request,
            CancellationToken cancellationToken)
        {
            var scripts = await _scriptRepository.GetAllDetailedAsync();

            return scripts.Select(x => new GetScriptListResponse
            {
                ScriptId = x.Id,
                Name = x.Name,
                Status = x.Status.ToString(),
                BatchName = x.Batch.Name,
                DeveloperName = x.Developer.Name,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}
