using DAL.Common;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Configurations
{
    public class ConflictMap : IEntityTypeConfiguration<Conflict>
    {
        public void Configure(EntityTypeBuilder<Conflict> builder)
        {
            builder.ToTable("Conflicts");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.TableName).IsRequired()
                .HasMaxLength(200);
            builder.Property(x => x.DetectedAt).IsRequired();

            builder.HasOne(x => x.Script)
                .WithMany(x => x.PrimaryConflicts)
                .HasForeignKey(x => x.ScriptId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ConflictingScript)
               .WithMany(x => x.SecondaryConflicts)
               .HasForeignKey(x => x.ConflictingScriptId)
               .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ResolvedByUser)
             .WithMany(x => x.ResolvedConflicts)
             .HasForeignKey(x => x.ResolvedBy)
             .OnDelete(DeleteBehavior.SetNull);
            builder.Property(x => x.Severity).IsRequired();
            builder.Property(x => x.Status).IsRequired();
        }
    }
}