using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid ProfileId { get; set; }
    public Guid ActorProfileId { get; set; }
    public string Type { get; set; } = null!;
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public string? PostPreview { get; set; }
    public string? CommentPreview { get; set; }
    public int TotalCount { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
    public Profile ActorProfile { get; set; } = null!;
    public Post? Post { get; set; }
    public Comment? Comment { get; set; }
}
