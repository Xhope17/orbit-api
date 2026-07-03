namespace Orbit.Application.Models.DTOs;

public record LikeResponse(
    Guid PostId,
    bool IsLiked,
    int LikeCount
);
