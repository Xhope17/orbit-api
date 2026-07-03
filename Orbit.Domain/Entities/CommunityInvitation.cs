using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class CommunityInvitation : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid ProfileId { get; set; }
    public Guid InvitedByProfileId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Community Community { get; set; } = null!;
    public Profile Profile { get; set; } = null!;
    public Profile InvitedBy { get; set; } = null!;
}
