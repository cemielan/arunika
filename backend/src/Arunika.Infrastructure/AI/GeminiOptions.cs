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
        "gemini-3.7-flash",
        "gemini-3.8-flash",
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
        ["gemini-3.8-flash"] = new ModelQuota { Rpm = 5, Tpm = 250_000, Rpd = 20 },
    };

    /// <summary>
    /// Number of articles to process before rotating to the next model.
    /// Set to 2 to distribute load across models and avoid rate limits.
    /// </summary>
    public int ArticlesPerRotation { get; set; } = 2;

    /// <summary>
    /// Max Gemini requests per rolling 60s window, summed across ALL models.
    /// This is a burst smoother on top of the per-model RPM budgets that
    /// <see cref="GeminiRateLimiter"/> derives from <see cref="ModelQuotas"/>,
    /// not a substitute for them — FetchNewsJob enqueues in bursts every 30
    /// minutes and this keeps the whole chain from firing at once.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 8;

    /// <summary>
    /// Percentage of each published quota the pipeline is allowed to spend, so
    /// it throttles itself before Google returns 429. 80 leaves the primary
    /// Flash-Lite models at 12 RPM / 400 RPD of their 15 / 500 allowance.
    /// </summary>
    public int QuotaSafetyPercent { get; set; } = 80;

    /// <summary>
    /// Attempts per model before falling through to the next one. Kept low on
    /// purpose: every attempt spends daily quota, and with a six-model chain a
    /// generous retry count multiplies one bad article into dozens of requests.
    /// </summary>
    public int MaxAttemptsPerModel { get; set; } = 2;

    /// <summary>
    /// How long a model is parked after the API reports a quota breach for it.
    /// Only ever applied to the offending model.
    /// </summary>
    public TimeSpan ModelQuotaCooldown { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Longest a caller will block waiting for a per-minute slot before the
    /// model is reported exhausted and the chain moves on. Bounded so an
    /// enrichment worker is never parked indefinitely behind one model.
    /// </summary>
    public TimeSpan MaxSlotWait { get; set; } = TimeSpan.FromSeconds(90);
}

public sealed class ModelQuota
{
    public int Rpm { get; set; }
    public int Tpm { get; set; }
    public int Rpd { get; set; }
}
