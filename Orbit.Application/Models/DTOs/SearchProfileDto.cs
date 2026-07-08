namespace Orbit.Application.Models.DTOs;

public record SearchProfileDto(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    bool IsFollowing
);
