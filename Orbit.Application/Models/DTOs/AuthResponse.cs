namespace Orbit.Application.Models.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    ProfileResponse Profile,
    List<string> Roles
);
