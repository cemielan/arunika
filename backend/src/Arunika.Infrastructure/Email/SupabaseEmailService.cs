using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

/// <summary>
/// Sends email through Supabase's Auth Admin <c>sendRawEmail</c> endpoint
/// (<c>POST {project-url}/auth/v1/admin/emails/raw</c>), which is delivered by
/// Supabase's infrastructure through the project's custom SMTP provider.
///
/// This is the only reliable path on Render's free tier, which blocks all direct
/// outbound SMTP ports (25/465/587). Two caveats are handled here:
/// <list type="bullet">
/// <item>GoTrue silently returns 200 and drops the email when the recipient does
/// not exist in <c>auth.users</c>. We therefore probe for the user first and fail
/// loudly instead of silently skipping. (Logged-in users always exist, since they
/// sign in through Supabase Auth.)</item>
/// <item>The service role key must only ever be used server-side.</item>
/// </list>
/// </summary>
public class SupabaseEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<SupabaseEmailService> logger) : IEmailService
{
    private const string UsersPath = "/auth/v1/admin/users";
    private const string SendPath = "/auth/v1/admin/emails/raw";

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.SupabaseUrl))
        {
            throw new InvalidOperationException("Supabase URL not configured. Set Email:SupabaseUrl.");
        }

        if (string.IsNullOrWhiteSpace(opts.SupabaseServiceRoleKey))
        {
            throw new InvalidOperationException("Supabase service role key not configured. Set Email:SupabaseServiceRoleKey.");
        }

        if (!await UserExistsAsync(to, opts, cancellationToken))
        {
            logger.LogWarning(
                "Email not sent: {To} is not a Supabase Auth user, so GoTrue would drop it. Subject: {Subject}", to, subject);
            throw new InvalidOperationException(
                $"'{to}' is not registered with Supabase Auth, so the email cannot be delivered through your SMTP provider.");
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

        logger.LogInformation("Email accepted by Supabase for delivery to {To}, subject: {Subject}", to, subject);
    }

    private async Task<bool> UserExistsAsync(string email, EmailOptions opts, CancellationToken cancellationToken)
    {
        var query = $"?filter={Uri.EscapeDataString(email)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{opts.SupabaseUrl.TrimEnd('/')}{UsersPath}{query}");
        request.Headers.Add("Authorization", $"Bearer {opts.SupabaseServiceRoleKey}");
        request.Headers.Add("apikey", opts.SupabaseServiceRoleKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // The probe itself failed (network, key, etc.); do not block the send on
            // a diagnostic failure — let sendRawEmail proceed and report its own result.
            return true;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("users", out var users))
        {
            return false;
        }

        foreach (var user in users.EnumerateArray())
        {
            if (user.TryGetProperty("email", out var emailEl)
                && string.Equals(emailEl.GetString(), email, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}