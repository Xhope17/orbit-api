using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class SavedPostConfiguration : IEntityTypeConfiguration<SavedPost>
{
    public void Configure(EntityTypeBuilder<SavedPost> builder)
    {
        builder.ToTable("saved_posts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(s => s.PostId)
            .HasColumnName("post_id")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(s => s.Profile)
            .WithMany(p => p.SavedPosts)
            .HasForeignKey(s => s.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Post)
            .WithMany(p => p.SavedBy)
            .HasForeignKey(s => s.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ProfileId, s.PostId })
            .IsUnique()
            .HasDatabaseName("ux_saved_posts_profile_post");

        builder.HasIndex(s => s.PostId)
            .HasDatabaseName("ix_saved_posts_post_id");

        builder.HasQueryFilter(s => s.Post.IsActive);
    }
}
