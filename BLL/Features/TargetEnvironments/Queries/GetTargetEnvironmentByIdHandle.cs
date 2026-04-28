using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.TargetEnvironments.Queries
{
    public class GetTargetEnvironmentByIdHandle
        : IRequestHandler<GetTargetEnvironmentByIdRequest, TargetEnvironmentResponse?>
    {
        private readonly ITargetEnvironmentRepository _repo;

        public GetTargetEnvironmentByIdHandle(ITargetEnvironmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<TargetEnvironmentResponse?> Handle(
            GetTargetEnvironmentByIdRequest request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id);
            if (entity == null || entity.IsDeleted) return null;

            return new TargetEnvironmentResponse
            {
                Id = entity.Id,
                ProjectName = entity.ProjectName,
                EnvironmentType = entity.EnvironmentType,
                Description = entity.Description,
                ConnectionString = entity.ConnectionString,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
