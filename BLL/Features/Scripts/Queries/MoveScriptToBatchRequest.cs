using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class MoveScriptToBatchRequest : IRequest<MoveScriptToBatchResponse>
    {
        public long ScriptId { get; set; }
        public long NewBatchId { get; set; }
    }
}