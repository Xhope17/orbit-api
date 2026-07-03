using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class CommunityInvitationConfiguration : IEntityTypeConfiguration<CommunityInvitation>
{
    public void Configure(EntityTypeBuilder<CommunityInvitation> builder)
    {
        builder.ToTable("community_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CommunityId)
            .HasColumnName("community_id")
            .IsRequired();

        builder.Property(i => i.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(i => i.InvitedByProfileId)
            .HasColumnName("invited_by_profile_id")
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("pending")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(i => i.RespondedAt)
            .HasColumnName("responded_at");

        builder.HasIndex(i => new { i.CommunityId, i.ProfileId })
            .IsUnique()
            .HasDatabaseName("ux_community_invitations_community_profile");

        builder.HasOne(i => i.Community)
            .WithMany()
            .HasForeignKey(i => i.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Profile)
            .WithMany()
            .HasForeignKey(i => i.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedByProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(i => i.Community.IsActive && i.Profile.IsActive);
    }
}
