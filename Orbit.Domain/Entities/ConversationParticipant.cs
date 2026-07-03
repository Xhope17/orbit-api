namespace Orbit.Domain.Entities;

public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Guid ProfileId { get; set; }
    public DateTime JoinedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Profile Profile { get; set; } = null!;
}
