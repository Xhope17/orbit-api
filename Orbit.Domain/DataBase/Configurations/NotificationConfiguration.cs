using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(n => n.ActorProfileId)
            .HasColumnName("actor_profile_id")
            .IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.PostId)
            .HasColumnName("post_id");

        builder.Property(n => n.CommentId)
            .HasColumnName("comment_id");

        builder.Property(n => n.PostPreview)
            .HasColumnName("post_preview")
            .HasMaxLength(200);

        builder.Property(n => n.CommentPreview)
            .HasColumnName("comment_preview")
            .HasMaxLength(200);

        builder.Property(n => n.TotalCount)
            .HasColumnName("total_count")
            .HasDefaultValue(1);

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(n => n.Profile)
            .WithMany()
            .HasForeignKey(n => n.ProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(n => n.ActorProfile)
            .WithMany()
            .HasForeignKey(n => n.ActorProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(n => n.Post)
            .WithMany()
            .HasForeignKey(n => n.PostId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(n => n.Comment)
            .WithMany()
            .HasForeignKey(n => n.CommentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(n => new { n.ProfileId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("ix_notifications_profile_read_created");
    }
}
