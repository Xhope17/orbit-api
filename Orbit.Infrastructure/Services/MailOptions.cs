namespace Orbit.Infrastructure.Services;

public class MailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string FromName { get; set; } = "Orbit";
    public string FromEmail { get; set; } = "noreply@orbitsocial.com";
}
