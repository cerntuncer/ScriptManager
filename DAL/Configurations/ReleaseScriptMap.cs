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
    public class ReleaseScriptMap : IEntityTypeConfiguration<ReleaseScript>
    {
        public void Configure(EntityTypeBuilder<ReleaseScript> builder)
        {
            builder.ToTable("ReleaseScripts");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.ExecutionOrder).IsRequired();


            builder.HasOne(x => x.Release)
                .WithMany(x => x.ReleaseScripts)
                .HasForeignKey(x => x.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Script)
               .WithMany(x => x.ReleaseScripts)
               .HasForeignKey(x => x.ScriptId)
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}