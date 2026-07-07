using System.Security.Cryptography;
using System.Text;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;

namespace Orbit.Application.Helpers;

public static class TokenHelper
{
    public static (string rawRefreshToken, UserSession session) CreateSession(Guid authUserId, IPasswordHasher passwordHasher)
    {
        var rawRefreshToken = GenerateRefreshToken();
        var refreshTokenHash = passwordHasher.Hash(rawRefreshToken);
        var tokenKey = ComputeTokenKey(rawRefreshToken);

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            RefreshTokenHash = refreshTokenHash,
            TokenKey = tokenKey,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        return (rawRefreshToken, session);
    }

    public static string ComputeTokenKey(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
