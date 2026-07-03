namespace Orbit.WebApi.Models;

public record RegisterRequest(
    string Email,
    string Username,
    string DisplayName,
    string Password,
    string? Bio,
    IFormFile? ProfilePicture
);
