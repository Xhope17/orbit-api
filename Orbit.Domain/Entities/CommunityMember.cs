using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class CommunityMember : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid ProfileId { get; set; }
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; }

    public Community Community { get; set; } = null!;
    public Profile Profile { get; set; } = null!;
}
