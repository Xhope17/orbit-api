namespace Orbit.WebApi.Models;

public record LoginRequest(
    string EmailOrUsername,
    string Password
);
