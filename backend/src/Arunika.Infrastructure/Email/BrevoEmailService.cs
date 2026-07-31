using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

public class BrevoEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    private const string SendEndpoint = "/smtp/email";

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning("Email not sent: Brevo API key not configured. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(opts.FromAddress))
        {
            logger.LogWarning("Email not sent: Brevo sender address not configured. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        var payload = new
        {
            sender = new { name = opts.FromName, email = opts.FromAddress },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("api-key", opts.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = body;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var messageEl))
                {
                    detail = messageEl.GetString() ?? body;
                }
            }
            catch (JsonException)
            {
                // body wasn't JSON; keep the raw response
            }

            logger.LogWarning("Brevo rejected email to {To}: {Status} {Detail}", to, (int)response.StatusCode, detail);
            throw new InvalidOperationException($"Brevo API returned {(int)response.StatusCode}: {detail}");
        }

        logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
    }
}
