using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.AuthUserId)
            .HasColumnName("auth_user_id")
            .IsRequired();

        builder.Property(p => p.Username)
            .HasColumnName("username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.UsernameSlug)
            .HasColumnName("username_slug")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Bio)
            .HasColumnName("bio")
            .HasMaxLength(500);

        builder.Property(p => p.ProfilePictureUrl)
            .HasColumnName("profile_picture_url")
            .HasMaxLength(1000);

        builder.Property(p => p.ProfilePicturePublicId)
            .HasColumnName("profile_picture_public_id")
            .HasMaxLength(500);

        builder.Property(p => p.BannerUrl)
            .HasColumnName("banner_url")
            .HasMaxLength(1000);

        builder.Property(p => p.BannerPublicId)
            .HasColumnName("banner_public_id")
            .HasMaxLength(500);

        builder.Property(p => p.PrefixId)
            .HasColumnName("prefix_id");

        builder.Property(p => p.PinnedPostId)
            .HasColumnName("pinned_post_id");

        builder.Property(p => p.FollowersCount)
            .HasColumnName("followers_count")
            .HasDefaultValue(0);

        builder.Property(p => p.FollowingCount)
            .HasColumnName("following_count")
            .HasDefaultValue(0);

        builder.Property(p => p.PostsCount)
            .HasColumnName("posts_count")
            .HasDefaultValue(0);

        builder.Property(p => p.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(p => p.IsPremium)
            .HasColumnName("is_premium")
            .HasDefaultValue(false);

        builder.Property(p => p.IsPrivate)
            .HasColumnName("is_private")
            .HasDefaultValue(false);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.IsBanned)
            .HasColumnName("is_banned")
            .HasDefaultValue(false);

        builder.Property(p => p.BannedAt)
            .HasColumnName("banned_at");

        builder.Property(p => p.BannedByProfileId)
            .HasColumnName("banned_by_profile_id");

        builder.HasOne(p => p.BannedBy)
            .WithMany()
            .HasForeignKey(p => p.BannedByProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(p => p.AuthUserId)
            .IsUnique()
            .HasDatabaseName("ux_profiles_auth_user");

        builder.HasIndex(p => p.Username)
            .IsUnique()
            .HasDatabaseName("ux_profiles_username");

        builder.HasIndex(p => p.UsernameSlug)
            .IsUnique()
            .HasDatabaseName("ux_profiles_username_slug");

        builder.HasOne(p => p.Prefix)
            .WithMany(up => up.Profiles)
            .HasForeignKey(p => p.PrefixId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(p => p.IsActive);
    }
}
