using System.Text;
using System.Text.Json;
using Orbit.Application.Interfaces.Services;

namespace Orbit.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly MailOptions _options;
    private readonly HttpClient _httpClient;

    public EmailService(MailOptions options, HttpClient httpClient)
    {
        _options = options;
        _httpClient = httpClient;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var payload = new
        {
            sender = new { name = _options.FromName, email = _options.FromEmail },
            to = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = htmlBody
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", _options.ApiKey);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Brevo API error: {response.StatusCode} - {errorBody}");
        }
    }
}
