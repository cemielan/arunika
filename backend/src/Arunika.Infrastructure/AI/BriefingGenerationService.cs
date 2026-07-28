using System.Text.Json;
using System.Text.Json.Serialization;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchemaType = Google.GenAI.Types.Type;

namespace Arunika.Infrastructure.AI;

public class BriefingGenerationService(
    IOptions<GeminiOptions> options,
    GeminiRateLimiter rateLimiter,
    ILogger<BriefingGenerationService> logger) : IBriefingGenerationService
{
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Schema ResponseSchema = new()
    {
        Type = SchemaType.Object,
        Properties = new Dictionary<string, Schema>
        {
            ["executiveSummary"] = new Schema
            {
                Type = SchemaType.String,
                Description = "2-3 paragraph executive summary of the day's top market news."
            },
            ["overallSentiment"] = new Schema
            {
                Type = SchemaType.String,
                Enum = ["Bullish", "Bearish", "Neutral"]
            },
            ["riskLevel"] = new Schema
            {
                Type = SchemaType.String,
                Enum = ["Low", "Medium", "High"]
            }
        },
        Required = ["executiveSummary", "overallSentiment", "riskLevel"]
    };

    private static readonly List<SafetySetting> SafetySettings =
    [
        new SafetySetting { Category = HarmCategory.HarmCategoryHarassment, Threshold = HarmBlockThreshold.BlockOnlyHigh },
        new SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech, Threshold = HarmBlockThreshold.BlockOnlyHigh },
        new SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.BlockOnlyHigh },
        new SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.BlockOnlyHigh },
    ];

    public async Task<BriefingGenerationResult> GenerateAsync(IReadOnlyList<Article> topStories, CancellationToken cancellationToken = default)
    {
        var client = new Client(apiKey: options.Value.ApiKey);
        var prompt = BuildPrompt(topStories);
        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = ResponseSchema,
            SafetySettings = SafetySettings
        };

        var modelsToTry = new List<string> { options.Value.Model };
        modelsToTry.AddRange(options.Value.FallbackModels);

        Exception? lastException = null;

        foreach (var model in modelsToTry)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    await rateLimiter.WaitForSlotAsync(cancellationToken);

                    var response = await client.Models.GenerateContentAsync(
                        model: model,
                        contents: prompt,
                        config: config,
                        cancellationToken: cancellationToken);

                    var text = response.Text
                        ?? throw new InvalidOperationException("Gemini returned no text for briefing.");

                    var payload = JsonSerializer.Deserialize<BriefingPayload>(text, JsonOptions)
                        ?? throw new InvalidOperationException("Gemini briefing response could not be parsed.");

                    logger.LogInformation(
                        "BriefingGenerationService: generated briefing using model {Model}.",
                        model);

                    return new BriefingGenerationResult(
                        payload.ExecutiveSummary,
                        Enum.Parse<Sentiment>(payload.OverallSentiment, ignoreCase: true),
                        Enum.Parse<RiskLevel>(payload.RiskLevel, ignoreCase: true));
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < MaxAttempts)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        logger.LogWarning(ex,
                            "Briefing generation attempt {Attempt}/{MaxAttempts} with model {Model} failed; retrying in {Delay}.",
                            attempt, MaxAttempts, model, delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(ex,
                            "Briefing generation exhausted {MaxAttempts} attempts with model {Model}; trying next fallback.",
                            MaxAttempts, model);
                    }
                }
            }
        }

        throw new InvalidOperationException("Briefing generation failed after exhausting all models.", lastException);
    }

    private static string BuildPrompt(IReadOnlyList<Article> topStories)
    {
        var storiesText = string.Join("\n\n", topStories.Select((a, i) =>
            $"Story {i + 1}: {a.Title}\nSummary: {a.Analysis?.Summary ?? "N/A"}\nCategory: {a.Analysis?.Category?.Name ?? "N/A"}\nImpact Score: {a.Analysis?.ImpactScore ?? 0}\nSentiment: {a.Analysis?.Sentiment}"));

        return $"""
            You are a financial market intelligence analyst. Below are the top market
            news stories for today, ranked by market impact. Generate a concise daily
            briefing.

            {storiesText}

            Provide:
            1. An executive summary (2-3 paragraphs) synthesizing the key themes.
            2. An overall market sentiment (Bullish, Bearish, or Neutral).
            3. A risk level (Low, Medium, or High) for the near-term outlook.
            """;
    }

    private sealed record BriefingPayload(
        [property: JsonPropertyName("executiveSummary")]
        string ExecutiveSummary,
        [property: JsonPropertyName("overallSentiment")]
        string OverallSentiment,
        [property: JsonPropertyName("riskLevel")]
        string RiskLevel);
}
