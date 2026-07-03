using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Follow : BaseEntity
{
    public Guid FollowerId { get; set; }
    public Guid FollowingId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Follower { get; set; } = null!;
    public Profile Following { get; set; } = null!;
}
