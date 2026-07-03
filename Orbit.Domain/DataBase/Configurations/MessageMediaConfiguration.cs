using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class MessageMediaConfiguration : IEntityTypeConfiguration<MessageMedia>
{
    public void Configure(EntityTypeBuilder<MessageMedia> builder)
    {
        builder.ToTable("message_media");

        builder.HasKey(mm => mm.Id);

        builder.Property(mm => mm.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(mm => mm.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(mm => mm.Url)
            .HasColumnName("url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(mm => mm.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(mm => mm.Width)
            .HasColumnName("width");

        builder.Property(mm => mm.Height)
            .HasColumnName("height");

        builder.Property(mm => mm.DurationSeconds)
            .HasColumnName("duration_seconds");

        builder.Property(mm => mm.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(100);

        builder.Property(mm => mm.SizeBytes)
            .HasColumnName("size_bytes");

        builder.Property(mm => mm.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(mm => mm.Message)
            .WithMany(m => m.MessageMedia)
            .HasForeignKey(mm => mm.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(mm => mm.Message.DeletedAt == null);
    }
}
