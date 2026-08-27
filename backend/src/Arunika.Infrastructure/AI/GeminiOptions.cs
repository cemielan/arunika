namespace Arunika.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Primary model. Text-out Flash/Pro models only — image (Nano Banana)
    /// and TTS models are unsuitable for news summarization/classification.
    /// </summary>
    public string Model { get; set; } = "gemini-3.7-flash";

    /// <summary>
    /// Newest-first fallback chain. Full-capability Flash models come first;
    /// Flash-Lite variants last because they carry the highest RPM/RPD quotas
    /// and are the safest workhorse once the newer models' small daily quotas
    /// (20 RPD each) run out. Pro models are omitted — this key's tier has
    /// zero Pro quota.
    /// </summary>
    public List<string> FallbackModels { get; set; } =
    [
        "gemini-3.6-flash",
        "gemini-3.5-flash",
        "gemini-3-flash",
        "gemini-3.5-flash-lite",
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash-lite",
    ];

    /// <summary>
    /// Max Gemini requests per rolling 60s window, shared across ALL models.
    /// Must stay below the tightest per-model RPM in the chain (5 RPM on the
    /// Flash models); kept at 4 to leave headroom for retries.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 4;
}
