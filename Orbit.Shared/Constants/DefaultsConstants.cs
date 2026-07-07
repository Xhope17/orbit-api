namespace Orbit.Shared.Constants;

public static class DefaultsConstants
{
    public const string JwtIssuer = "OrbitApi";
    public const string JwtAudience = "OrbitClient";
    public const int JwtAccessTokenExpirationMinutes = 1440; // 1 día
    public const int JwtRefreshTokenExpirationDays = 15;

    public const string RedisConnection = "localhost:6379";

    public const string SmtpHost = "smtp-relay.brevo.com";
    public const int SmtpPort = 587;
    public const string SmtpFromName = "Orbit";
    public const string SmtpFromEmail = "noreply@orbitsocial.com";

    public const string FrontendUrl = "http://localhost:3000";
}
