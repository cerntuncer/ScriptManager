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
    public class GetScriptCommitsHandle
         : IRequestHandler<GetScriptCommitsRequest, List<GetScriptCommitsResponse>>
    {
        private readonly IRepository<Commit> _commitRepository;

        public GetScriptCommitsHandle(IRepository<Commit> commitRepository)
        {
            _commitRepository = commitRepository;
        }

        public async Task<List<GetScriptCommitsResponse>> Handle(
            GetScriptCommitsRequest request,
            CancellationToken cancellationToken)
        {
            var commits = await _commitRepository
                .GetWhereAsync(x => x.ScriptId == request.ScriptId);

            var ordered = commits
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return ordered.Select(x => new GetScriptCommitsResponse
            {
                CommitId = x.Id,
                UserName = x.User.Name,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}