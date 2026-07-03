namespace Orbit.Application.Models.DTOs;

public record PostResponse(
    Guid Id,
    PostAuthorResponse Author,
    string? Content,
    List<PostMediaResponse> Media,
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
    PostResponse? OriginalPost
);
