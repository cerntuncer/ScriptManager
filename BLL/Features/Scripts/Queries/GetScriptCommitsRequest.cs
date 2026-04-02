using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class GetScriptCommitsRequest : IRequest<List<GetScriptCommitsResponse>>
    {
        public long ScriptId { get; set; }
    }
}