using System.Security.Claims;

namespace Orbit.Application.Interfaces.Services;

public interface IJwtService
{
    (string token, DateTime expiresAt) GenerateAccessToken(Guid authUserId, Guid profileId, string username, List<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
