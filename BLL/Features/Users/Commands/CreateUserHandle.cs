using BLL.Features.User.Commands;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Users.Commands
{
    public class CreateUserHandle : IRequestHandler<CreateUserRequest, CreateUserResponse>
    {
        private readonly IUserRepository _userRepository;

        public CreateUserHandle(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var user = new DAL.Entities.User
            {
                Name = request.Name?.Trim() ?? string.Empty,
                Email = request.Email?.Trim() ?? string.Empty,
                Role = UserRole.Developer,
                IsActive = true
            };
            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();
            return new CreateUserResponse
            {
                Success = true,
                Message = "Kullanıcı oluşturuldu.",
                UserId = user.Id
            };
        }
    }
}
