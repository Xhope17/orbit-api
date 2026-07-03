namespace Orbit.Application.Models.DTOs;

public record PostAuthorResponse(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    bool IsFollowing
);
