using DAL.Common;
using DAL.Enums;

namespace DAL.Entities
{
    public class TargetEnvironment : BaseEntity
    {
        public string ProjectName { get; set; } = null!;
        public EnvironmentType EnvironmentType { get; set; }
        public string ConnectionString { get; set; } = null!;
        public string? Description { get; set; }
    }
}
