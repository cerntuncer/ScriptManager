using DAL.Enums;

namespace BLL.Features.TargetEnvironments.Queries
{
    public class TargetEnvironmentResponse
    {
        public long Id { get; init; }
        public string ProjectName { get; init; } = "";
        public EnvironmentType EnvironmentType { get; init; }
        public string EnvironmentTypeName => EnvironmentType.ToString();
        public string? Description { get; init; }
        public string ConnectionString { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
