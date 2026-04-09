using MediatR;

namespace BLL.Features.Releases.Commands;

public class CancelReleaseRequest : IRequest<CancelReleaseResponse>
{
    public long ReleaseId { get; set; }
    public IReadOnlyList<long>? ReleaseIds { get; set; }
}
