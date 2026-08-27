namespace Arunika.Infrastructure.AI;

public class AiOptions
{
    public const string SectionName = "AI";

    /// <summary>
    /// Primary AI provider used by the composite services. Supported values:
    /// "Gemini" (default) or "OpenRouter". The other provider acts as fallback.
    /// </summary>
    public string PreferredProvider { get; set; } = "Gemini";
}
