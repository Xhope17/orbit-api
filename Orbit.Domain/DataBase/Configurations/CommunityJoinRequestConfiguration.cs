using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class CommunityJoinRequestConfiguration : IEntityTypeConfiguration<CommunityJoinRequest>
{
    public void Configure(EntityTypeBuilder<CommunityJoinRequest> builder)
    {
        builder.ToTable("community_join_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CommunityId)
            .HasColumnName("community_id")
            .IsRequired();

        builder.Property(r => r.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("pending")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(r => r.RespondedAt)
            .HasColumnName("responded_at");

        builder.HasIndex(r => new { r.CommunityId, r.ProfileId })
            .IsUnique()
            .HasDatabaseName("ux_community_join_requests_community_profile");

        builder.HasOne(r => r.Community)
            .WithMany()
            .HasForeignKey(r => r.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Profile)
            .WithMany()
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(r => r.Community.IsActive && r.Profile.IsActive);
    }
}
