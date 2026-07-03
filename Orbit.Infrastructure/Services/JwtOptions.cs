namespace Orbit.Infrastructure.Services;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "OrbitApi";
    public string Audience { get; set; } = "OrbitClient";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
