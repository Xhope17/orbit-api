using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class UserPrefixConfiguration : IEntityTypeConfiguration<UserPrefix>
{
    public void Configure(EntityTypeBuilder<UserPrefix> builder)
    {
        builder.ToTable("user_prefixes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Color)
            .HasColumnName("color")
            .HasMaxLength(20);

        builder.Property(p => p.IconUrl)
            .HasColumnName("icon_url")
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("ux_user_prefixes_name");
    }
}
