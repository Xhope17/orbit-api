namespace Orbit.Application.Models.DTOs;

public record PostDto(
    Guid Id,
    PostAuthorDto Author,
    string? Content,
    List<PostMediaDto> Media,
    int LikeCount,
    int CommentCount,
    int SaveCount,
    bool IsLiked,
    bool IsSaved,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsRepost,
    bool IsThread,
    Guid? OriginalPostId,
    PostDto? OriginalPost
);
