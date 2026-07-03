using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderProfileId { get; set; }
    public string? Content { get; set; }
    public bool IsSeen { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Profile SenderProfile { get; set; } = null!;
    public ICollection<MessageMedia> MessageMedia { get; set; } = [];
}
