namespace Arunika.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP server settings (e.g. smtp.gmail.com on port 587).</summary>
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>Sender address shown in the recipient's inbox.</summary>
    public string FromEmail { get; set; } = "arunika.noreply@gmail.com";

    public string FromName { get; set; } = "Arunika";

    public string FrontendUrl { get; set; } = "http://localhost:3000";
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}