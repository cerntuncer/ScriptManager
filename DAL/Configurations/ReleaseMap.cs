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
    public class ReleaseMap : IEntityTypeConfiguration<Release>
    {
        public void Configure(EntityTypeBuilder<Release> builder)
        {
            builder.ToTable("Releases");
            BaseMap.ConfigureBase(builder);


            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);


            builder.Property(x => x.Version)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.Version).IsUnique();

            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.HasOne(x => x.Creator)
                .WithMany(x => x.CreatedReleases)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Status).IsRequired();
        }
    }
}