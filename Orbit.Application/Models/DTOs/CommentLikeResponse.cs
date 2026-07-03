namespace Orbit.Application.Models.DTOs;

public record CommentLikeResponse(
    Guid CommentId,
    bool IsLiked,
    int LikeCount
);
