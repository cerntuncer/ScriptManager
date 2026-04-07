using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Users.Queries
{
    public class GetUserListHandle : IRequestHandler<GetUserListRequest, List<GetUserListItemResponse>>
    {
        private readonly IUserRepository _userRepository;

        public GetUserListHandle(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<GetUserListItemResponse>> Handle(GetUserListRequest request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync();
            return users
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.Name)
                .Select(u => new GetUserListItemResponse
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email
                })
                .ToList();
        }
    }
}
