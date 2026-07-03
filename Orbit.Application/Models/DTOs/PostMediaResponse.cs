namespace Orbit.Application.Models.DTOs;

public record PostMediaResponse(
    string Url,
    string MediaType,
    int Order,
    int? Width,
    int? Height,
    long? SizeBytes,
    string? Format,
    int? DurationSeconds
);
