using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("post_likes");

        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(pl => pl.PostId)
            .HasColumnName("post_id")
            .IsRequired();

        builder.Property(pl => pl.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(pl => pl.Profile)
            .WithMany()
            .HasForeignKey(pl => pl.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(pl => pl.Post)
            .WithMany(p => p.PostLikes)
            .HasForeignKey(pl => pl.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pl => new { pl.ProfileId, pl.PostId })
            .IsUnique()
            .HasDatabaseName("ux_post_likes_profile_post");

        builder.HasIndex(pl => pl.PostId)
            .HasDatabaseName("ix_post_likes_post_id");

        builder.HasQueryFilter(pl => pl.Post.IsActive);
    }
}
