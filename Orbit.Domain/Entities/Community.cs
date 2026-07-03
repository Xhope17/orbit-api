using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Community : BaseEntity, ISoftDeletable
{
    public Guid OwnerProfileId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public int MemberCount { get; set; }
    public bool IsPrivate { get; set; }
    public string? BannerUrl { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Profile Owner { get; set; } = null!;
    public ICollection<CommunityMember> Members { get; set; } = [];
    public ICollection<Post> Posts { get; set; } = [];
}
