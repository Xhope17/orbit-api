namespace Orbit.Shared.Constants;

public static class EnvironmentConstants
{
    public const string DefaultConnection = "CONNECTIONSTRINGS__DEFAULTCONNECTION";
    public const string DefaultConnectionAlt = "ConnectionStrings__DefaultConnection";

    public const string JwtSecret = "JWT__SECRET";
    public const string JwtIssuer = "JWT__ISSUER";
    public const string JwtAudience = "JWT__AUDIENCE";
    public const string JwtAccessTokenExpiration = "JWT__ACCESSTOKENEXPIRATIONMINUTES";
    public const string JwtRefreshTokenExpiration = "JWT__REFRESHTOKENEXPIRATIONDAYS";

    public const string CloudinaryCloudName = "CLOUDINARY_CLOUD_NAME";
    public const string CloudinaryApiKey = "CLOUDINARY_API_KEY";
    public const string CloudinaryApiSecret = "CLOUDINARY_API_SECRET";

    public const string RedisConnection = "REDIS_CONNECTION";

    public const string SmtpHost = "SMTP__HOST";
    public const string SmtpPort = "SMTP__PORT";
    public const string SmtpUsername = "SMTP__USERNAME";
    public const string SmtpPassword = "SMTP__PASSWORD";
    public const string SmtpFromName = "SMTP__FROMNAME";
    public const string SmtpFromEmail = "SMTP__FROMEMAIL";

    public const string BrevoApiKey = "BREVO_API_KEY";
    public const string FrontendUrl = "FRONTEND_URL";
    public const string FrontendUrlDev = "FRONTEND_URL_DEV";
}
