using BLL.Common;
using BLL.Features.Batchs.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class UpdateScriptRequest : IRequest<UpdateScriptResponse>
    {
        public long ScriptId { get; set; }

        public string? Name { get; set; }
        public string? SqlScript { get; set; }
        public string? RollbackScript { get; set; }

        public long? BatchId { get; set; }
        public CreateBatchRequest? Batch { get; set; }
        public long? ReleaseId { get; set; }

        public long UserId { get; set; } // commit için
        public string? Message { get; set; } // commit mesajı

        public int? Status { get; set; } // enum int olarak
    }
}