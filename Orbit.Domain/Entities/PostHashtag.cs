using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class PostHashtag : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid HashtagId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Post Post { get; set; } = null!;
    public Hashtag Hashtag { get; set; } = null!;
}
