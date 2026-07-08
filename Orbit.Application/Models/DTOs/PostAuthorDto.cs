namespace Orbit.Application.Models.DTOs;

public record PostAuthorDto(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    bool IsFollowing
);
