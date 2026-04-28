using MediatR;

namespace BLL.Features.TargetEnvironments.Queries
{
    public class GetTargetEnvironmentsRequest : IRequest<List<TargetEnvironmentResponse>>
    {
        public string? ProjectName { get; set; }
    }
}
