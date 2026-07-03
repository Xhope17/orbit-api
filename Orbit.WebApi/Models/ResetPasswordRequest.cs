namespace Orbit.WebApi.Models;

public record ResetPasswordRequest(string Username, string Token, string NewPassword);
