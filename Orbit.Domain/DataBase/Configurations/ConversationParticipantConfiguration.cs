using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("conversation_participants");

        builder.HasKey(cp => new { cp.ConversationId, cp.ProfileId });

        builder.Property(cp => cp.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(cp => cp.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(cp => cp.JoinedAt)
            .HasColumnName("joined_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(cp => cp.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Profile)
            .WithMany()
            .HasForeignKey(cp => cp.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(cp => cp.Profile.IsActive);
    }
}
