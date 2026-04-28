using DAL.Enums;
using MediatR;

namespace BLL.Features.TargetEnvironments.Commands
{
    public class UpdateTargetEnvironmentRequest : IRequest<TargetEnvironmentCommandResponse>
    {
        public long Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public EnvironmentType EnvironmentType { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
