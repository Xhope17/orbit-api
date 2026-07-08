using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;
using Orbit.Application.Models.Helpers;
using Orbit.Domain.Entities;
using Orbit.Shared.Constants;

namespace Orbit.Application.Helpers;

public static class TokenHelper
{
    public static TokenConfiguration Configuration(IConfiguration configuration)
    {
        var secret = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtSecret)
            ?? configuration[ConfigurationConstants.JwtSecret]
            ?? string.Empty;

        var issuer = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtIssuer)
            ?? configuration[ConfigurationConstants.JwtIssuer]
            ?? DefaultsConstants.JwtIssuer;

        var audience = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtAudience)
            ?? configuration[ConfigurationConstants.JwtAudience]
            ?? DefaultsConstants.JwtAudience;

        var expirationMinutes = int.TryParse(
            Environment.GetEnvironmentVariable(EnvironmentConstants.JwtAccessTokenExpiration)
            ?? configuration[ConfigurationConstants.JwtAccessTokenExpiration],
            out var minutes)
            ? minutes : DefaultsConstants.JwtAccessTokenExpirationMinutes;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var now = DateTime.UtcNow;
        var timespan = TimeSpan.FromMinutes(expirationMinutes);

        return new TokenConfiguration
        {
            Issuer = issuer,
            Audience = audience,
            SecurityKey = securityKey,
            Expiration = now.Add(timespan),
            ExpirationTimeSpan = timespan
        };
    }

    public static string Create(Guid authUserId, Guid profileId, string username, List<string> roles, TokenConfiguration config)
    {
        var signingCredentials = new SigningCredentials(config.SecurityKey, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, authUserId.ToString()),
            new(ClaimConstants.ProfileId, profileId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claimsList.Add(new Claim(ClaimTypes.Role, role));
        }

        var securityToken = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claimsList,
            expires: config.Expiration,
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }

    public static ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, TokenConfiguration config)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config.Issuer,
            ValidAudience = config.Audience,
            IssuerSigningKey = config.SecurityKey,
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

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
            ExpiresAt = DateTime.UtcNow.AddDays(DefaultsConstants.JwtRefreshTokenExpirationDays),
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
