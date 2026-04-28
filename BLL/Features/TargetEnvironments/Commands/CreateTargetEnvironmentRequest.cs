using DAL.Enums;
using MediatR;

namespace BLL.Features.TargetEnvironments.Commands
{
    public class CreateTargetEnvironmentRequest : IRequest<TargetEnvironmentCommandResponse>
    {
        public string ProjectName { get; set; } = string.Empty;
        public EnvironmentType EnvironmentType { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
