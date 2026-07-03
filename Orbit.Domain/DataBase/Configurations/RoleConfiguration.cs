using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("ux_roles_name");

        var seedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Role { Id = Guid.Parse("00000001-0000-0000-0000-000000000001"), Name = "admin", CreatedAt = seedDate },
            new Role { Id = Guid.Parse("00000001-0000-0000-0000-000000000002"), Name = "moderator", CreatedAt = seedDate },
            new Role { Id = Guid.Parse("00000001-0000-0000-0000-000000000003"), Name = "user", CreatedAt = seedDate }
        );
    }
}
