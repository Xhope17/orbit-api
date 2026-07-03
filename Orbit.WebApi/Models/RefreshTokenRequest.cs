namespace Orbit.WebApi.Models;

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);
