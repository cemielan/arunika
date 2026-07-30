namespace Arunika.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@arunika.app";
    public string FromName { get; set; } = "Arunika";
    public string FrontendUrl { get; set; } = "http://localhost:3000";
    public int SmtpTimeoutSeconds { get; set; } = 15;
}
