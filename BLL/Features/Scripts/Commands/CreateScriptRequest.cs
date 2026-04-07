using BLL.Features.Batchs.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptRequest : IRequest<CreateScriptResponse>
    {
        public string Name { get; set; }
        public string SqlScript { get; set; }
        public string? RollbackScript { get; set; }
        public long? BatchId { get; set; }
        public CreateBatchRequest? Batch { get; set; }
        public long DeveloperId { get; set; }

    }
}
