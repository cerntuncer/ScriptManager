using BLL.Common;
using MediatR;

namespace BLL.Features.Releases.Commands;

public class SetReleaseTreeLockRequest : IRequest<SetReleaseTreeLockResponse>
{
    public long ReleaseId { get; set; }
    /// <summary>true: tüm ağaç batchleri kilitlenir; false: düzenlemeye açılır.</summary>
    public bool Lock { get; set; }
}

public class SetReleaseTreeLockResponse : BaseResponse
{
}
