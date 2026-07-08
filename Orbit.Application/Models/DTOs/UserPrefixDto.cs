namespace Orbit.Application.Models.DTOs;

public record UserPrefixDto(
    Guid Id,
    string Name,
    string? Color,
    string? IconUrl
);
