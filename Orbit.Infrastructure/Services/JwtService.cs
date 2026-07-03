using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;

namespace Orbit.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtOptions _options;

    public JwtService(JwtOptions options)
    {
        _options = options;
    }

    public (string token, DateTime expiresAt) GenerateAccessToken(Guid authUserId, Guid profileId, string username, List<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

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

        var claims = claimsList.ToArray();

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = key,
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
}
