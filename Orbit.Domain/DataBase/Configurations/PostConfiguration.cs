using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(p => p.CommunityId)
            .HasColumnName("community_id");

        builder.Property(p => p.Content)
            .HasColumnName("content")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(p => p.LikeCount)
            .HasColumnName("like_count")
            .HasDefaultValue(0);

        builder.Property(p => p.CommentCount)
            .HasColumnName("comment_count")
            .HasDefaultValue(0);

        builder.Property(p => p.SaveCount)
            .HasColumnName("save_count")
            .HasDefaultValue(0);

        builder.Property(p => p.IsRepost)
            .HasColumnName("is_repost")
            .HasDefaultValue(false);

        builder.Property(p => p.IsThread)
            .HasColumnName("is_thread")
            .HasDefaultValue(false);

        builder.Property(p => p.OriginalPostId)
            .HasColumnName("original_post_id");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => p.ProfileId)
            .HasDatabaseName("ix_posts_profile_id");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("ix_posts_created_at");

        builder.HasIndex(p => p.CommunityId)
            .HasDatabaseName("ix_posts_community_id");

        builder.HasOne(p => p.Profile)
            .WithMany()
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Community)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CommunityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.OriginalPost)
            .WithMany()
            .HasForeignKey(p => p.OriginalPostId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.OriginalPostId)
            .HasDatabaseName("ix_posts_original_post_id");

        builder.HasQueryFilter(p => p.IsActive);
    }
}
