namespace Orbit.Application.Models.DTOs;

public record CloudinaryUploadResult(
    string Url,
    string PublicId,
    int? Width,
    int? Height,
    long? SizeBytes,
    string? Format,
    int? DurationSeconds
);
