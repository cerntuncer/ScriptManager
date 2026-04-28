using DAL.Entities;
using DAL.Repositories.Interfaces;
using MediatR;

namespace BLL.Features.TargetEnvironments.Queries
{
    public class GetTargetEnvironmentsHandle
        : IRequestHandler<GetTargetEnvironmentsRequest, List<TargetEnvironmentResponse>>
    {
        private readonly ITargetEnvironmentRepository _repo;

        public GetTargetEnvironmentsHandle(ITargetEnvironmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<TargetEnvironmentResponse>> Handle(
            GetTargetEnvironmentsRequest request, CancellationToken cancellationToken)
        {
            List<TargetEnvironment> environments;

            if (!string.IsNullOrWhiteSpace(request.ProjectName))
                environments = await _repo.GetByProjectNameAsync(request.ProjectName.Trim());
            else
                environments = await _repo.GetAllActiveAsync();

            return environments.Select(Map).ToList();
        }

        private static TargetEnvironmentResponse Map(TargetEnvironment e) => new()
        {
            Id = e.Id,
            ProjectName = e.ProjectName,
            EnvironmentType = e.EnvironmentType,
            Description = e.Description,
            ConnectionString = e.ConnectionString,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
