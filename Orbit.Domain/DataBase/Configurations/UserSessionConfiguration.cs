using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AuthUserId)
            .HasColumnName("auth_user_id")
            .IsRequired();

        builder.Property(s => s.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.TokenKey)
            .HasColumnName("token_key")
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(s => s.AuthUserId)
            .HasDatabaseName("ix_user_sessions_auth_user");

        builder.HasIndex(s => s.TokenKey)
            .HasDatabaseName("ix_user_sessions_token_key")
            .IsUnique()
            .HasFilter("[token_key] IS NOT NULL");

        builder.HasQueryFilter(s => s.AuthUser.IsActive);

        builder.HasOne(s => s.AuthUser)
            .WithMany(u => u.UserSessions)
            .HasForeignKey(s => s.AuthUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
