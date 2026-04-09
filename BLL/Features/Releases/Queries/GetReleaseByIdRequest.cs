using MediatR;

namespace BLL.Features.Releases.Queries
{
    public class GetReleaseByIdRequest : IRequest<GetReleaseByIdResponse>
    {
        public long ReleaseId { get; set; }
    }
}
