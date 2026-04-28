using MediatR;

namespace BLL.Features.TargetEnvironments.Queries
{
    public class GetTargetEnvironmentByIdRequest : IRequest<TargetEnvironmentResponse?>
    {
        public long Id { get; set; }
    }
}
