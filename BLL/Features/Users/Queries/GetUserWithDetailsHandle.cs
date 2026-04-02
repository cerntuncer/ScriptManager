using DAL.Repositories.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetUserWithDetailsHandle : IRequestHandler<GetUserWithDetailsRequest, GetUserWithDetailsResponse>
    {
        private readonly IUserRepository _userRepository;

        public GetUserWithDetailsHandle(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<GetUserWithDetailsResponse> Handle(
            GetUserWithDetailsRequest request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserWithDetailsAsync(request.Id);

            if (user == null)
            {
                return new GetUserWithDetailsResponse
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı."
                };
            }

            return new GetUserWithDetailsResponse
            {
                Success = true,
                Message = "Kullanıcı detayları getirildi.",

                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,

                ScriptCount = user.Scripts?.Count ?? 0,
                CommitCount = user.Commits?.Count ?? 0,
                ResolvedConflictCount = user.ResolvedConflicts?.Count ?? 0
            };
        }
    }
}