using DAL.Common;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Configurations
{
    public class UserMap : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            BaseMap.ConfigureBase(builder);
            builder.Property(x => x.Name).IsRequired()
                .HasMaxLength(150);
            builder.Property(x => x.Email).IsRequired()
                .HasMaxLength(200);
            builder.Property(x => x.Role).IsRequired();

            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.Email).IsUnique();
        }

    }
}