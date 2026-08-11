using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

/// <summary>
/// Sends email through Supabase's Auth Admin <c>sendRawEmail</c> endpoint
/// (<c>POST {project-url}/auth/v1/admin/emails/raw</c>). Requires the service
/// role key, so this must only ever run server-side.
/// </summary>
public class SupabaseEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<SupabaseEmailService> logger) : IEmailService
{
    private const string SendPath = "/auth/v1/admin/emails/raw";

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.SupabaseUrl))
        {
            logger.LogWarning("Email not sent: Supabase URL not configured. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(opts.SupabaseServiceRoleKey))
        {
            logger.LogWarning("Email not sent: Supabase service role key not configured. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        var payload = new
        {
            to = to,
            subject,
            html_body = htmlBody,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{opts.SupabaseUrl.TrimEnd('/')}{SendPath}")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("Authorization", $"Bearer {opts.SupabaseServiceRoleKey}");
        request.Headers.Add("apikey", opts.SupabaseServiceRoleKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = body;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("msg", out var msgEl))
                {
                    detail = msgEl.GetString() ?? body;
                }
            }
            catch (JsonException)
            {
                // body wasn't JSON; keep the raw response
            }

            logger.LogWarning("Supabase rejected email to {To}: {Status} {Detail}", to, (int)response.StatusCode, detail);
            throw new InvalidOperationException($"Supabase sendRawEmail returned {(int)response.StatusCode}: {detail}");
        }

        logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
    }
}
