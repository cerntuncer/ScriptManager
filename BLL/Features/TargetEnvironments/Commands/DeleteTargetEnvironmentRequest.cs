using MediatR;

namespace BLL.Features.TargetEnvironments.Commands
{
    public class DeleteTargetEnvironmentRequest : IRequest<TargetEnvironmentCommandResponse>
    {
        public long Id { get; set; }
    }
}
