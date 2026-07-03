using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class UserBanConfiguration : IEntityTypeConfiguration<UserBan>
{
    public void Configure(EntityTypeBuilder<UserBan> builder)
    {
        builder.ToTable("user_bans", t => t.HasCheckConstraint("chk_user_bans_self", "blocker_profile_id <> blocked_profile_id"));

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BlockerProfileId)
            .HasColumnName("blocker_profile_id")
            .IsRequired();

        builder.Property(b => b.BlockedProfileId)
            .HasColumnName("blocked_profile_id")
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(b => b.Blocker)
            .WithMany()
            .HasForeignKey(b => b.BlockerProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.Blocked)
            .WithMany()
            .HasForeignKey(b => b.BlockedProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(b => new { b.BlockerProfileId, b.BlockedProfileId })
            .IsUnique()
            .HasDatabaseName("ux_user_bans_blocker_blocked");

        builder.HasIndex(b => b.BlockerProfileId)
            .HasDatabaseName("ix_user_bans_blocker");

        builder.HasIndex(b => b.BlockedProfileId)
            .HasDatabaseName("ix_user_bans_blocked");

        builder.HasQueryFilter(b => b.Blocker.IsActive && b.Blocked.IsActive);
    }
}
