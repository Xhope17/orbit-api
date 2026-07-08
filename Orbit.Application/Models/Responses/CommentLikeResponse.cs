namespace Orbit.Application.Models.Responses;

public record CommentLikeResponse(Guid CommentId, bool IsLiked, int LikeCount);
