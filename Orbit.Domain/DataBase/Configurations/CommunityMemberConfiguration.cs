using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class CommunityMemberConfiguration : IEntityTypeConfiguration<CommunityMember>
{
    public void Configure(EntityTypeBuilder<CommunityMember> builder)
    {
        builder.ToTable("community_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.CommunityId)
            .HasColumnName("community_id")
            .IsRequired();

        builder.Property(m => m.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasDefaultValue("member")
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(m => new { m.CommunityId, m.ProfileId })
            .IsUnique()
            .HasDatabaseName("ux_community_members_community_profile");

        builder.HasOne(m => m.Community)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Profile)
            .WithMany()
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasCheckConstraint("chk_community_member_role", "role IN ('owner', 'co_leader', 'member')");

        builder.HasQueryFilter(m => m.Community.IsActive && m.Profile.IsActive);
    }
}
