namespace Orbit.Application.Models.Responses;

public record PostLikeResponse(Guid PostId, bool IsLiked, int LikeCount);
