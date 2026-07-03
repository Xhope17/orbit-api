using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ur => ur.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(ur => ur.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(ur => new { ur.ProfileId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("ux_user_roles_profile_role");

        builder.HasOne(ur => ur.Profile)
            .WithMany(p => p.UserRoles)
            .HasForeignKey(ur => ur.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(ur => ur.Profile.IsActive);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
