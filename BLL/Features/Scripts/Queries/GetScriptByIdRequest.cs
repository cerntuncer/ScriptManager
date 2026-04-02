using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class GetScriptByIdRequest : IRequest<GetScriptByIdResponse>
    {
        public long ScriptId { get; set; }
    }
}