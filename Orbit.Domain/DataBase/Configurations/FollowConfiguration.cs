using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FollowerId)
            .HasColumnName("follower_id")
            .IsRequired();

        builder.Property(f => f.FollowingId)
            .HasColumnName("following_id")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(f => new { f.FollowerId, f.FollowingId })
            .IsUnique()
            .HasDatabaseName("ux_follows_follower_following");

        builder.HasIndex(f => f.FollowerId)
            .HasDatabaseName("ix_follows_follower");

        builder.HasIndex(f => f.FollowingId)
            .HasDatabaseName("ix_follows_following");

        builder.HasQueryFilter(f => f.Follower.IsActive && f.Following.IsActive);
    }
}
