using DAL.Common;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Configurations;

public class ConflictPairDismissalMap : IEntityTypeConfiguration<ConflictPairDismissal>
{
    public void Configure(EntityTypeBuilder<ConflictPairDismissal> builder)
    {
        builder.ToTable("ConflictPairDismissals");
        BaseMap.ConfigureBase(builder);

        builder.Property(x => x.SqlFingerprintMin).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SqlFingerprintMax).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ResolutionKind).HasConversion<int>();

        builder.HasIndex(x => new { x.ScriptIdMin, x.ScriptIdMax });

        builder.HasOne(x => x.ScriptMin)
            .WithMany()
            .HasForeignKey(x => x.ScriptIdMin)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ScriptMax)
            .WithMany()
            .HasForeignKey(x => x.ScriptIdMax)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ResolvedByUser)
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
