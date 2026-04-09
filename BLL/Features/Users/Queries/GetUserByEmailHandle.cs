using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.Users.Queries
{
    public class GetUserByEmailHandle : IRequestHandler<GetUserByEmailRequest, GetUserByEmailResponse>
    {
        private readonly IUserRepository _userRepository;
        public GetUserByEmailHandle(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<GetUserByEmailResponse> Handle(GetUserByEmailRequest request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new GetUserByEmailResponse
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı."
                };
            return new GetUserByEmailResponse
            {
                Success = true,
                Message = "Kullanıcı bulundu.",
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,


            };
        }
    }
}