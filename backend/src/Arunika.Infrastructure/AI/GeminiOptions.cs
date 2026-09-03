namespace Arunika.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Primary model. Flash-Lite models have highest quotas on free tier.
    /// Gemini 3.1 Flash Lite: 15 RPM, 250K TPM, 500 RPD (highest).
    /// Gemini 3.5 Flash Lite: 15 RPM, 250K TPM, 500 RPD (highest).
    /// </summary>
    public string Model { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// Fallback chain ordered by quota sustainability (highest RPM/TPM/RPD first).
    /// Primary: gemini-3.5-flash-lite (15 RPM, 250K TPM, 500 RPD)
    /// Secondary: gemini-3.1-flash-lite (15 RPM, 250K TPM, 500 RPD)
    /// Tertiary: gemini-2.5-flash-lite (10 RPM, 250K TPM, 20 RPD)
    /// Flash models (lower RPD): gemini-3.6-flash, gemini-3.5-flash, gemini-3-flash
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
    /// Model quota configuration for rate limiting per model.
    /// Key: model name, Value: (RPM, TPM, RPD)
    /// </summary>
    public Dictionary<string, ModelQuota> ModelQuotas { get; set; } = new()
    {
        ["gemini-3.1-flash-lite"] = new ModelQuota { Rpm = 15, Tpm = 250_000, Rpd = 500 },
        ["gemini-3.5-flash-lite"] = new ModelQuota { Rpm = 15, Tpm = 250_000, Rpd = 500 },
        ["gemini-2.5-flash-lite"] = new ModelQuota { Rpm = 10, Tpm = 250_000, Rpd = 20 },
        ["gemini-3.7-flash"] = new ModelQuota { Rpm = 5, Tpm = 250_000, Rpd = 20 },
        ["gemini-3.6-flash"] = new ModelQuota { Rpm = 5, Tpm = 250_000, Rpd = 20 },
        ["gemini-3.5-flash"] = new ModelQuota { Rpm = 5, Tpm = 250_000, Rpd = 20 },
        ["gemini-3-flash"] = new ModelQuota { Rpm = 5, Tpm = 250_000, Rpd = 20 },
        ["gemini-2.5-flash"] = new ModelQuota { Rpm = 5, Tpm = 250_000, Rpd = 20 },
    };

    /// <summary>
    /// Number of articles to process before rotating to the next model.
    /// Set to 2 to distribute load across models and avoid rate limits.
    /// </summary>
    public int ArticlesPerRotation { get; set; } = 2;

    /// <summary>
    /// Max Gemini requests per rolling 60s window, shared across ALL models.
    /// Must stay below the tightest per-model RPM in the chain (5 RPM on the
    /// Flash models); kept at 4 to leave headroom for retries.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 4;
}

public sealed class ModelQuota
{
    public int Rpm { get; set; }
    public int Tpm { get; set; }
    public int Rpd { get; set; }
}
