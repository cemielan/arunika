using System.Net;
using System.Net.Mail;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

/// <summary>
/// Sends email directly over SMTP (Gmail) instead of routing through Supabase's
/// GoTrue admin API <c>sendRawEmail</c>, which silently drops messages for
/// addresses that do not exist in the project's <c>auth.users</c> table. Direct
/// SMTP can reach any valid recipient (e.g. logged-in users in our own Users
/// table) regardless of GoTrue membership.
/// </summary>
public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var smtp = opts.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host)
            || string.IsNullOrWhiteSpace(smtp.Username)
            || string.IsNullOrWhiteSpace(smtp.Password))
        {
            logger.LogWarning("Email not sent: SMTP not configured. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password),
            Timeout = 15000,
        };

        using var message = new MailMessage
        {
            From = new MailAddress(opts.FromEmail, opts.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
    }
}