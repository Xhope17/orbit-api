namespace Orbit.Application.Models.DTOs;

public record RegisterResponse(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string? Bio
);
