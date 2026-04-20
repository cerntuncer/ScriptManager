using MediatR;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptRequest : IRequest<CreateScriptResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
        public long DeveloperId { get; set; }

        public long ActorUserId { get; set; }

        public long? BatchId { get; set; }
    }
}
