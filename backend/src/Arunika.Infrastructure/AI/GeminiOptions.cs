namespace Arunika.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Primary model. Flash-Lite models have 30 RPM / 1000+ RPD quotas on free tier,
    /// making them the only viable choice for production volume. Flash models
    /// (3.5-flash, 3.6-flash, etc.) only allow 20 RPD and exhaust instantly.
    /// </summary>
    public string Model { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// Fallback chain ordered by quota sustainability (highest RPD first).
    /// Flash-Lite models: 30 RPM, 1000+ RPD. Flash models: 15 RPM, 20 RPD.
    /// </summary>
    public List<string> FallbackModels { get; set; } =
    [
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash-lite",
        "gemini-3.6-flash",
        "gemini-3.5-flash",
        "gemini-3-flash",
    ];

    /// <summary>
    /// Max Gemini requests per rolling 60s window, shared across ALL models.
    /// Must stay below the tightest per-model RPM in the chain (5 RPM on the
    /// Flash models); kept at 4 to leave headroom for retries.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 4;
}
