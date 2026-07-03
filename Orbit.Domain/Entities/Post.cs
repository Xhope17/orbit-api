using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Post : BaseEntity, ISoftDeletable
{
    public Guid ProfileId { get; set; }
    public Guid? CommunityId { get; set; }
    public string Content { get; set; } = null!;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int SaveCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsRepost { get; set; }
    public bool IsThread { get; set; }
    public Guid? OriginalPostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
    public Community? Community { get; set; }
    public Post? OriginalPost { get; set; }
    public ICollection<PostMedia> PostMedia { get; set; } = [];
    public ICollection<PostLike> PostLikes { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<SavedPost> SavedBy { get; set; } = [];
}
