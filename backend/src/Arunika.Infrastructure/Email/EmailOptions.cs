namespace Arunika.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Shared secret that must be sent with every call to the Supabase edge
    /// function (must match the function's SEND_SECRET env var).
    /// </summary>
    public string EdgeFunctionSecret { get; set; } = string.Empty;

    /// <summary>Name of the deployed edge function (defaults to send-digest-email).</summary>
    public string EdgeFunctionName { get; set; } = "send-digest-email";

    public string FromName { get; set; } = "Arunika";

    public string FrontendUrl { get; set; } = "http://localhost:3000";
}