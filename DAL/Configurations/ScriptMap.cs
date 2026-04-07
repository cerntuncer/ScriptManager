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
    public class ScriptMap : IEntityTypeConfiguration<Script>
    {
        public void Configure(EntityTypeBuilder<Script> builder)
        {
            builder.ToTable("Scripts");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.Name).IsRequired()
                .HasMaxLength(200);
            builder.Property(x => x.SqlScript).IsRequired();
            builder.Property(x => x.RollbackScript);
            builder.Property(x => x.BatchId).IsRequired(false);
            builder.HasOne(x => x.Batch)
                .WithMany(x => x.Scripts)
                .HasForeignKey(x => x.BatchId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            builder.HasOne(x => x.Developer)
               .WithMany(x => x.Scripts)
               .HasForeignKey(x => x.DeveloperId)
               .OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.StatusBeforeConflict)
                .HasConversion<int>()
                .IsRequired(false);
            builder.HasIndex(x => x.Name);
        }
    }
}