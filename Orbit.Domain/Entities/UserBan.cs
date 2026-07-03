using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class UserBan : BaseEntity
{
    public Guid BlockerProfileId { get; set; }
    public Guid BlockedProfileId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Blocker { get; set; } = null!;
    public Profile Blocked { get; set; } = null!;
}
