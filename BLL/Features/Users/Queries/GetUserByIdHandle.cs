using BLL.Features.User.Commands;
using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetUserByIdHandle : IRequestHandler<GetUserByIdRequest, GetUserByIdResponse>
    {
        private readonly IUserRepository _userRepository;
        public GetUserByIdHandle(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<GetUserByIdResponse> Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                return new GetUserByIdResponse
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı."
                };
            }
            return new GetUserByIdResponse
            {
                Success = true,
                Message = "Kullanıcı getirildi.",
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive

            };
        }
    }
}