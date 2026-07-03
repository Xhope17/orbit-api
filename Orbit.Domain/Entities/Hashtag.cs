using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Hashtag : BaseEntity
{
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public ICollection<PostHashtag> PostHashtags { get; set; } = [];
}
