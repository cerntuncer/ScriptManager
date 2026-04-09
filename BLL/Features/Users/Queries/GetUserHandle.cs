using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Users.Queries
{
    public class GetUserHandle : IRequestHandler<GetUserRequest, GetUserResponse>
    {
        private readonly IUserRepository _userRepository;

        public GetUserHandle(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<GetUserResponse> Handle(GetUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserWithDetailsAsync(request.Id);
            if (user == null)
            {
                return new GetUserResponse
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı."
                };
            }

            return new GetUserResponse
            {
                Success = true,
                Message = "Kullanıcı getirildi.",
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                ScriptCount = user.Scripts?.Count ?? 0,
                ResolvedConflictCount = user.ResolvedConflicts?.Count ?? 0
            };
        }
    }
}
