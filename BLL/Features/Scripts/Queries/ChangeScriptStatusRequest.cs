using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Queries
{
    public class ChangeScriptStatusRequest : IRequest<ChangeScriptStatusResponse>
    {
        public long ScriptId { get; set; }

        public int NewStatus { get; set; } // enum int
        public int UserId { get; set; }

        public string? Message { get; set; }
    }
}