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
    public class UserCredentialMap : IEntityTypeConfiguration<UserCredential>
    {
        public void Configure(EntityTypeBuilder<UserCredential> builder)
        {
            builder.ToTable("UserCredentials");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.UserName).IsRequired()
                .HasMaxLength(100);
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.HasOne(x => x.User)
                .WithOne(x => x.Credential)
                .HasForeignKey<UserCredential>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}