using BLL.Common;
using MediatR;

namespace BLL.Features.Conflicts.Commands;

public class ResolveConflictRequest : IRequest<ResolveConflictResponse>
{
    public long ConflictId { get; set; }
    public long UserId { get; set; }
}

public class ResolveConflictResponse : BaseResponse
{
    public long ConflictId { get; set; }
}
