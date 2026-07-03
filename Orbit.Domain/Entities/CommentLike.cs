using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class CommentLike : BaseEntity
{
    public Guid ProfileId { get; set; }
    public Guid CommentId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
    public Comment Comment { get; set; } = null!;
}
