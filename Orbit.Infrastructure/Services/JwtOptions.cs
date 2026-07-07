namespace Orbit.Infrastructure.Services;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "OrbitApi";
    public string Audience { get; set; } = "OrbitClient";
    public int AccessTokenExpirationMinutes { get; set; } = 1440; // 1 día
    public int RefreshTokenExpirationDays { get; set; } = 15;
}
