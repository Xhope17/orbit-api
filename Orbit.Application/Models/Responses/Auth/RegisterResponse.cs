namespace Orbit.Application.Models.Responses.Auth;

public record RegisterResponse(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string? Bio
);
