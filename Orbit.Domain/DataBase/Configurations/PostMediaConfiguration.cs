using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("post_media");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.PostId)
            .HasColumnName("post_id")
            .IsRequired();

        builder.Property(pm => pm.Url)
            .HasColumnName("url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(pm => pm.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pm => pm.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pm => pm.Order)
            .HasColumnName("order")
            .HasDefaultValue(0);

        builder.Property(pm => pm.Width)
            .HasColumnName("width");

        builder.Property(pm => pm.Height)
            .HasColumnName("height");

        builder.Property(pm => pm.SizeBytes)
            .HasColumnName("size_bytes");

        builder.Property(pm => pm.Format)
            .HasColumnName("format")
            .HasMaxLength(20);

        builder.Property(pm => pm.DurationSeconds)
            .HasColumnName("duration_seconds");

        builder.Property(pm => pm.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(pm => pm.Post)
            .WithMany(p => p.PostMedia)
            .HasForeignKey(pm => pm.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pm => pm.PostId)
            .HasDatabaseName("ix_post_media_post_id");

        builder.HasQueryFilter(pm => pm.Post.IsActive);
    }
}
