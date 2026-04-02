using BLL.Features.User.Commands;
using DAL.Enums;
using DAL.Repositories.Interfaces;
using MediatR;
using DAL.Entities;

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
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = "İsim boş olamaz."
                };
            }
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = "Email boş olamaz."
                };
            }
            var isExist = await _userRepository.IsEmailExistAsync(request.Email);
            if (isExist)
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = "Bu email zaten kayıtlı."
                };
            }
            var user = new DAL.Entities.User
            {
                Name = request.Name,
                Email = request.Email,
                Role = UserRole.Developer,
                IsActive = true
            };
            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();
            return new CreateUserResponse
            {
                Success = true,
                Message = "Kullanıcı başarıyla oluşturuldu.",
                UserId = user.Id
            };
        }
    }
}