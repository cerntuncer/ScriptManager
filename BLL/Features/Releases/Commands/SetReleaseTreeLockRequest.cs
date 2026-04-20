using BLL.Common;
using MediatR;

namespace BLL.Features.Releases.Commands;

public class SetReleaseTreeLockRequest : IRequest<SetReleaseTreeLockResponse>
{
    public long ReleaseId { get; set; }
    public bool Lock { get; set; }
}

public class SetReleaseTreeLockResponse : BaseResponse
{
}
