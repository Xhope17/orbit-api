namespace Orbit.WebApi.Models;

public record UpdateProfileRequest(
    string? DisplayName,
    string? Bio,
    bool? IsPrivate
);
