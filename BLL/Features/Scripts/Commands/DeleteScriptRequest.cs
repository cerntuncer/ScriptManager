using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class DeleteScriptRequest : IRequest<DeleteScriptResponse>
    {
        public long ScriptId { get; set; }

        public int UserId { get; set; }
        public string? Message { get; set; }
    }
}