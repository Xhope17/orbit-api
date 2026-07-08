namespace Orbit.Application.Models.DTOs;

public record CommentDto(
    Guid Id,
    PostAuthorDto Author,
    string Content,
    Guid? ParentCommentId,
    int ReplyCount,
    int LikeCount,
    bool IsLiked,
    DateTime CreatedAt
);
