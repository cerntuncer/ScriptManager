using MediatR;

namespace BLL.Features.Releases.Commands
{
    public class CreateReleaseRequest : IRequest<CreateReleaseResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public long CreatedBy { get; set; }

        public string? InitialBatchName { get; set; }

        public List<long>? SourcePoolBatchRootIds { get; set; }

        public long? RestrictScriptAssignmentToDeveloperId { get; set; }
    }
}
