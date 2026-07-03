using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
{
    public void Configure(EntityTypeBuilder<PostHashtag> builder)
    {
        builder.ToTable("post_hashtags");

        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.PostId)
            .HasColumnName("post_id")
            .IsRequired();

        builder.Property(ph => ph.HashtagId)
            .HasColumnName("hashtag_id")
            .IsRequired();

        builder.Property(ph => ph.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(ph => ph.Post)
            .WithMany()
            .HasForeignKey(ph => ph.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ph => ph.Hashtag)
            .WithMany(h => h.PostHashtags)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ph => new { ph.PostId, ph.HashtagId })
            .IsUnique()
            .HasDatabaseName("ix_post_hashtags_post_hashtag");

        builder.HasIndex(ph => ph.HashtagId)
            .HasDatabaseName("ix_post_hashtags_hashtag_id");
    }
}
