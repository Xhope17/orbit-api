namespace Orbit.Application.Models.DTOs;

public record BlockedUserDto(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    DateTime BlockedAt
);
