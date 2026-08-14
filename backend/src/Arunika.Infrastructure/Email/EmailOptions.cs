namespace Arunika.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Supabase project URL, e.g. https://abcdefgh.supabase.co.</summary>
    public string SupabaseUrl { get; set; } = string.Empty;

    /// <summary>Supabase service role key (server-side only).</summary>
    public string SupabaseServiceRoleKey { get; set; } = string.Empty;

    public string FromName { get; set; } = "Arunika";

    public string FrontendUrl { get; set; } = "http://localhost:3000";
}