using DAL.Common;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Configurations
{
    public class TargetEnvironmentMap : IEntityTypeConfiguration<TargetEnvironment>
    {
        public void Configure(EntityTypeBuilder<TargetEnvironment> builder)
        {
            builder.ToTable("TargetEnvironments");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.ProjectName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.EnvironmentType).IsRequired().HasConversion<int>();
            builder.Property(x => x.ConnectionString).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.HasIndex(x => new { x.ProjectName, x.EnvironmentType }).IsUnique();
        }
    }
}
