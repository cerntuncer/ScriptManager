using MediatR;

namespace BLL.Features.Releases.Queries
{
    public class GetReleasesRequest : IRequest<List<GetReleaseListItemResponse>>
    {
    }
}
