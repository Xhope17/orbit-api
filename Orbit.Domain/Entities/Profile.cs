using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Profile : BaseEntity, ISoftDeletable
{
    public Guid AuthUserId { get; set; }
    public string Username { get; set; } = null!;
    public string UsernameSlug { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? ProfilePicturePublicId { get; set; }
    public string? BannerUrl { get; set; }
    public string? BannerPublicId { get; set; }
    public Guid? PrefixId { get; set; }
    public Guid? PinnedPostId { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostsCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsPremium { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsActive { get; set; }
    public bool IsBanned { get; set; }
    public DateTime? BannedAt { get; set; }
    public Guid? BannedByProfileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AuthUser AuthUser { get; set; } = null!;
    public UserPrefix? Prefix { get; set; }
    public Profile? BannedBy { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<SavedPost> SavedPosts { get; set; } = [];
}
