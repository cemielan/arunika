using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Application.Abstractions;
using Arunika.Infrastructure.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

/// <summary>
/// Sends email by calling a Supabase Edge Function over HTTPS
/// (<c>POST {project-url}/functions/v1/{name}</c>), which in turn delivers it via
/// Gmail SMTP. This is the only path that works on Render's free tier, which
/// blocks all outbound SMTP ports (25/465/587): the edge function runs on
/// Supabase's infrastructure where those ports are reachable, and our backend
/// only ever talks to Supabase over port 443.
/// </summary>
public class EdgeFunctionEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    IOptions<SupabaseOptions> supabaseOptions,
    ILogger<EdgeFunctionEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.EdgeFunctionSecret))
        {
            throw new InvalidOperationException(
                "Edge function secret not configured. Set Email:EdgeFunctionSecret to the same value as the function's SEND_SECRET.");
        }

        var url = $"{supabaseOptions.Value.Url.TrimEnd('/')}/functions/v1/{opts.EdgeFunctionName}";

        var payload = new
        {
            to = to,
            subject,
            html = htmlBody,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("x-send-secret", opts.EdgeFunctionSecret);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Edge function rejected email to {To}: {Status} {Body}", to, (int)response.StatusCode, body);
            throw new InvalidOperationException($"Edge function returned {(int)response.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.False)
        {
            var detail = doc.RootElement.TryGetProperty("error", out var errorEl) && errorEl.ValueKind == JsonValueKind.String
                ? errorEl.GetString()
                : body;
            logger.LogWarning("Edge function failed for {To}: {Detail}", to, detail);
            throw new InvalidOperationException($"Edge function failed: {detail}");
        }

        logger.LogInformation("Email accepted by edge function for {To}, subject: {Subject}", to, subject);
    }
}