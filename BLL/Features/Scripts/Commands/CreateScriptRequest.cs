using MediatR;

namespace BLL.Features.Scripts.Commands
{
    public class CreateScriptRequest : IRequest<CreateScriptResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
        public long DeveloperId { get; set; }

        /// <summary>Oturum / denetim için.</summary>
        public long ActorUserId { get; set; }

        /// <summary>Havuz batch'i (yaprak) veya null = batch atanmamış script.</summary>
        public long? BatchId { get; set; }
    }
}
