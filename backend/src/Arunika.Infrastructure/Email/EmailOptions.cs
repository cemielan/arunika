namespace Arunika.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Supabase service role key (server-side only). The Supabase project URL is
    /// reused from the <c>Supabase:Url</c> section (already required for JWT auth).
    /// </summary>
    public string SupabaseServiceRoleKey { get; set; } = string.Empty;

    public string FromName { get; set; } = "Arunika";

    public string FrontendUrl { get; set; } = "http://localhost:3000";
}