namespace Arunika.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Arunika";
    public string FrontendUrl { get; set; } = "http://localhost:3000";
}
