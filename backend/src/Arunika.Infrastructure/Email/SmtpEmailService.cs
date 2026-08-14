using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.Email;

/// <summary>
/// Sends email directly over SMTP (Gmail) using a raw <see cref="TcpClient"/>
/// + <see cref="SslStream"/> client instead of <see cref="System.Net.Mail.SmtpClient"/>.
/// This is deliberate:
/// <list type="bullet">
/// <item><c>SmtpClient</c> always connects to the first DNS result, and Gmail
/// publishes AAAA (IPv6) records first — environments without IPv6 routing hang
/// until timeout. This client pins the connection to an IPv4 address.</item>
/// <item>It does not depend on Supabase GoTrue, so any valid recipient address
/// (including logged-in users in our own Users table) can be emailed.</item>
/// </list>
/// </summary>
public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var smtp = opts.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host)
            || string.IsNullOrWhiteSpace(smtp.Username)
            || string.IsNullOrWhiteSpace(smtp.Password))
        {
            logger.LogWarning(
                "Email not sent: SMTP not configured. Set Email:Smtp:Host, Email:Smtp:Username and Email:Smtp:Password. "
                + "To: {To}, Subject: {Subject}", to, subject);
            throw new InvalidOperationException("SMTP is not configured. Set Email:Smtp:Host, Email:Smtp:Username and Email:Smtp:Password.");
        }

        var hostAddresses = await Dns.GetHostAddressesAsync(smtp.Host, cancellationToken);
        var address = hostAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
        if (address is null)
        {
            throw new InvalidOperationException($"SMTP host '{smtp.Host}' resolved to no IPv4 address.");
        }

        using var tcpClient = new TcpClient { NoDelay = true };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(OperationTimeout);

        try
        {
            await tcpClient.ConnectAsync(address, smtp.Port, timeoutCts.Token);
            var stream = await GetStreamAsync(tcpClient.GetStream(), smtp, timeoutCts.Token);
            var ehloName = EhloName(opts.FromEmail);

            if (smtp.Port != 465 && smtp.EnableSsl)
            {
                await EhloAsync(stream, ehloName, timeoutCts.Token);
                if (await CommandAsync(stream, "STARTTLS", timeoutCts.Token) != 220)
                {
                    throw new SmtpSendException("SMTP server did not accept STARTTLS.");
                }

                stream = await GetStreamAsync(stream, smtp, timeoutCts.Token);
            }

            await EhloAsync(stream, ehloName, timeoutCts.Token);
            await AuthenticateAsync(stream, smtp, opts.FromEmail, timeoutCts.Token);

            if (await CommandAsync(stream, $"MAIL FROM:<{opts.FromEmail}>", timeoutCts.Token) != 250)
            {
                throw new SmtpSendException("SMTP server rejected the sender address.");
            }

            if (await CommandAsync(stream, $"RCPT TO:<{to}>", timeoutCts.Token) != 250)
            {
                throw new SmtpSendException("SMTP server rejected the recipient address.");
            }

            if (await CommandAsync(stream, "DATA", timeoutCts.Token) != 354)
            {
                throw new SmtpSendException("SMTP server did not accept DATA.");
            }

            var message = BuildMessage(opts, to, subject, htmlBody);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(message), timeoutCts.Token);
            await stream.WriteAsync("\r\n.\r\n"u8.ToArray(), timeoutCts.Token);

            var (code, replyText) = await ReadReplyAsync(stream, timeoutCts.Token);
            if (code != 250)
            {
                throw new SmtpSendException($"SMTP server rejected the message body: {code} {replyText.Trim()}");
            }

            _ = await CommandAsync(stream, "QUIT", timeoutCts.Token);
            logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SmtpSendException("SMTP operation timed out while talking to " + $"{smtp.Host}:{smtp.Port}.");
        }
        catch (SocketException ex)
        {
            throw new SmtpSendException($"Could not connect to SMTP server {smtp.Host}:{smtp.Port} — {ex.Message}");
        }
    }

    private static async Task<Stream> GetStreamAsync(Stream stream, SmtpOptions smtp, CancellationToken ct)
    {
        if (smtp.Port != 465)
        {
            return stream;
        }

        var ssl = new SslStream(stream, false);
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = smtp.Host,
            EnabledSslProtocols = SslProtocols.None,
        }, ct);
        return ssl;
    }

    private static async Task EhloAsync(Stream stream, string ehloName, CancellationToken ct)
    {
        if (await CommandAsync(stream, $"EHLO {ehloName}", ct) != 250)
        {
            throw new SmtpSendException("SMTP server rejected the EHLO greeting.");
        }
    }

    private static async Task AuthenticateAsync(Stream stream, SmtpOptions smtp, string fromEmail, CancellationToken ct)
    {
        var credentials = "\0" + smtp.Username + "\0" + smtp.Password;
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        var (code, replyText) = await TryCommandAsync(stream, $"AUTH PLAIN {token}", ct);
        if (code == 235)
        {
            return;
        }

        // Gmail may require XOAUTH2-style App Passwords; surface the server's reason verbatim.
        throw new SmtpSendException(
            $"SMTP authentication failed using {smtp.Username} (sender {fromEmail}): {code} {replyText.Trim()}");
    }

    private static async Task<int> CommandAsync(Stream stream, string command, CancellationToken ct)
        => (await TryCommandAsync(stream, command, ct)).code;

    private static async Task<(int code, string text)> TryCommandAsync(Stream stream, string command, CancellationToken ct)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(command + "\r\n"), ct);
        return await ReadReplyAsync(stream, ct);
    }

    private static async Task<(int code, string text)> ReadReplyAsync(Stream stream, CancellationToken ct)
    {
        var text = new StringBuilder();
        var code = 0;
        while (true)
        {
            var line = await ReadLineAsync(stream, ct);
            if (line.Length < 3 || !int.TryParse(line.AsSpan(0, 3), out code))
            {
                throw new SmtpSendException($"Unexpected SMTP response line: '{line}'");
            }

            text.Append(line).Append('\n');
            if (line.Length < 4 || line[3] != '-')
            {
                return (code, text.ToString());
            }
        }
    }

    private static async Task<string> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var line = new MemoryStream();
        var buffer = new byte[1];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, ct);
            if (read == 0)
            {
                throw new SmtpSendException("SMTP server closed the connection unexpectedly.");
            }

            if (buffer[0] == (byte)'\n')
            {
                break;
            }

            line.WriteByte(buffer[0]);
        }

        return Encoding.ASCII.GetString(line.ToArray()).TrimEnd('\r');
    }

    private static string BuildMessage(EmailOptions opts, string to, string subject, string htmlBody)
    {
        var sb = new StringBuilder();
        sb.Append("From: ").Append(MimeEncode(opts.FromName)).Append(" <").Append(opts.FromEmail).Append(">\r\n");
        sb.Append("To: <").Append(to).Append(">\r\n");
        sb.Append("Subject: ").Append(MimeEncode(subject)).Append("\r\n");
        sb.Append("Date: ").Append(DateTimeOffset.UtcNow.ToString("R")).Append("\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/html; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit\r\n");
        sb.Append("\r\n").Append(htmlBody);
        return sb.ToString();
    }

    private static string MimeEncode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return $"=?UTF-8?B?{Convert.ToBase64String(bytes)}?=";
    }

    private static string EhloName(string fromEmail)
    {
        var at = fromEmail.IndexOf('@');
        return at > 0 ? fromEmail[(at + 1)..] : "localhost";
    }
}

/// <summary>
/// SMTP-level failure with the server's reply text preserved for diagnostics.
/// </summary>
public sealed class SmtpSendException(string message) : Exception(message);