using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class SavedPost : BaseEntity
{
    public Guid ProfileId { get; set; }
    public Guid PostId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
