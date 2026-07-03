namespace Orbit.Application.Interfaces.Services;

public interface IResetTokenService
{
    Task SaveTokenAsync(string email, string token, TimeSpan expiration);
    Task<string?> GetTokenAsync(string email);
    Task RemoveTokenAsync(string email);
}
