using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;
using Orbit.Shared.Constants;

namespace Orbit.Domain.DataBase.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.SenderProfileId)
            .HasColumnName("sender_profile_id")
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasMaxLength(DomainConstants.MessageContentMaxLength);

        builder.Property(m => m.IsSeen)
            .HasColumnName("is_seen")
            .HasDefaultValue(false);

        builder.Property(m => m.IsEdited)
            .HasColumnName("is_edited")
            .HasDefaultValue(false);

        builder.Property(m => m.EditedAt)
            .HasColumnName("edited_at");

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(m => m.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.SenderProfile)
            .WithMany()
            .HasForeignKey(m => m.SenderProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("ix_messages_conversation_id");

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_messages_conversation_created");

        builder.HasQueryFilter(m => m.DeletedAt == null);
    }
}
