using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class AuthUserConfiguration : IEntityTypeConfiguration<AuthUser>
{
    public void Configure(EntityTypeBuilder<AuthUser> builder)
    {
        builder.ToTable("auth_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.IsEmailVerified)
            .HasColumnName("is_email_verified")
            .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_auth_users_email");

        builder.HasOne(u => u.Profile)
            .WithOne(p => p.AuthUser)
            .HasForeignKey<Profile>(p => p.AuthUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(u => u.IsActive);
    }
}
