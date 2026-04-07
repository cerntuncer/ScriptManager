using MediatR;

namespace BLL.Features.Users.Queries
{
    public class GetUserListRequest : IRequest<List<GetUserListItemResponse>>
    {
    }
}
