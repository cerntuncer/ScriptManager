using DAL.Common;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace DAL.Configurations
{
    public class CommitMap : IEntityTypeConfiguration<Commit>
    {
        public void Configure(EntityTypeBuilder<Commit> builder)
        {
            builder.ToTable("Commits");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.HasOne(x => x.Script)
                .WithMany(x => x.Commits)
                .HasForeignKey(x => x.ScriptId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User)
               .WithMany(x => x.Commits)
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.Type).IsRequired();


        }
    }
}