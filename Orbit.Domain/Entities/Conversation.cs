using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Conversation : BaseEntity
{
    public string ConversationType { get; set; } = "dm";
    public DateTime CreatedAt { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
