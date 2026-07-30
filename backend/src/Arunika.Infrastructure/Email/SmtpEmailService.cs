using System.Net;
using System.Net.Mail;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.Username) || string.IsNullOrWhiteSpace(opts.Password))
        {
            logger.LogWarning("Email not sent: SMTP credentials not configured. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        // SmtpClient.Timeout only applies to synchronous Send(), not
        // SendMailAsync. Use a CancellationTokenSource to enforce the timeout
        // on the async path instead.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(opts.SmtpTimeoutSeconds));

        using var client = new SmtpClient(opts.SmtpHost, opts.SmtpPort)
        {
            Credentials = new NetworkCredential(opts.Username, opts.Password),
            EnableSsl = opts.UseSsl,
        };

        using var message = new MailMessage
        {
            From = new MailAddress(opts.FromAddress, opts.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        await client.SendMailAsync(message, timeoutCts.Token);

        logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
    }
}
