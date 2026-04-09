using MediatR;

namespace BLL.Features.Users.Queries
{
    public class GetUserRequest : IRequest<GetUserResponse>
    {
        public long Id { get; set; }
    }
}
