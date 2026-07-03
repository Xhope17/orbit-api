using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.ToTable("comment_likes");

        builder.HasKey(cl => cl.Id);

        builder.Property(cl => cl.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(cl => cl.CommentId)
            .HasColumnName("comment_id")
            .IsRequired();

        builder.Property(cl => cl.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(cl => cl.Profile)
            .WithMany()
            .HasForeignKey(cl => cl.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cl => cl.Comment)
            .WithMany(c => c.CommentLikes)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cl => new { cl.ProfileId, cl.CommentId })
            .IsUnique()
            .HasDatabaseName("ux_comment_likes_profile_comment");

        builder.HasIndex(cl => cl.CommentId)
            .HasDatabaseName("ix_comment_likes_comment_id");

        builder.HasQueryFilter(cl => cl.Comment.IsActive);
    }
}
