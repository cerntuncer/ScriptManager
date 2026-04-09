using BLL.Common;

namespace BLL.Features.Releases.Commands;

public class CancelReleaseResponse : BaseResponse
{
    public int RequestedCount { get; set; }
    public int CancelledCount { get; set; }
    public IReadOnlyList<long> FailedReleaseIds { get; set; } = Array.Empty<long>();
}
