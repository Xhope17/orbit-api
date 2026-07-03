using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("communities");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.OwnerProfileId)
            .HasColumnName("owner_profile_id")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.MemberCount)
            .HasColumnName("member_count")
            .HasDefaultValue(0);

        builder.Property(c => c.IsPrivate)
            .HasColumnName("is_private")
            .HasDefaultValue(false);

        builder.Property(c => c.BannerUrl)
            .HasColumnName("banner_url")
            .HasMaxLength(1000);

        builder.Property(c => c.IconUrl)
            .HasColumnName("icon_url")
            .HasMaxLength(1000);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("ux_communities_slug");

        builder.HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => c.IsActive);
    }
}
