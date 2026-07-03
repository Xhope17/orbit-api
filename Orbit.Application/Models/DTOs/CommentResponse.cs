namespace Orbit.Application.Models.DTOs;

public record CommentResponse(
    Guid Id,
    PostAuthorResponse Author,
    string Content,
    Guid? ParentCommentId,
    int ReplyCount,
    int LikeCount,
    bool IsLiked,
    DateTime CreatedAt
);
