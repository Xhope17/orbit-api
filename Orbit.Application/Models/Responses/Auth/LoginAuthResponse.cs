using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Models.Responses.Auth;

public record LoginAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    ProfileDto Profile,
    List<string> Roles
);
