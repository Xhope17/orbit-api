namespace Orbit.Application.Models.DTOs;

public record UserPrefixResponse(
    Guid Id,
    string Name,
    string? Color,
    string? IconUrl
);
