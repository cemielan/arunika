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

/// <summary>
/// Wraps the Gemini Developer API behind <see cref="IAiEnrichmentService"/>
/// (design doc §5): one structured-output call per article, combining
/// summary, classification, sentiment, impact score, sector impact, and
/// keywords, instead of six separate calls.
/// </summary>
public class GeminiAiEnrichmentService(
    IOptions<GeminiOptions> options,
    GeminiRateLimiter rateLimiter,
    ILogger<GeminiAiEnrichmentService> logger)
    : IAiEnrichmentService
{
    private const int MaxAttempts = 3;
    private const int MaxContentChars = 6000;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Schema ResponseSchema = BuildResponseSchema();

    // News reporting on litigation, violence, war, fraud, etc. is legitimate,
    // objective source material for this platform and routinely trips Gemini's
    // default (very conservative) safety thresholds, causing the response to
    // come back with no candidates/text (see HandleBlockedResponse below).
    // Loosen — but don't disable — the categories most likely to false-positive
    // on financial/political news coverage.
    private static readonly List<SafetySetting> SafetySettings =
    [
        new SafetySetting { Category = HarmCategory.HarmCategoryHarassment, Threshold = HarmBlockThreshold.BlockOnlyHigh },
        new SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech, Threshold = HarmBlockThreshold.BlockOnlyHigh },
        new SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.BlockOnlyHigh },
        new SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.BlockOnlyHigh },
    ];

    public async Task<ArticleAnalysisResult> AnalyzeAsync(Article article, CancellationToken cancellationToken = default)
    {
        var client = new Client(apiKey: options.Value.ApiKey);
        var prompt = BuildPrompt(article);
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

                    LogTokenUsage(article.Id, model, response);

                    var text = response.Text
                        ?? throw new InvalidOperationException(DescribeEmptyResponse(response));
                    var payload = JsonSerializer.Deserialize<GeminiAnalysisPayload>(text, JsonOptions)
                        ?? throw new InvalidOperationException("Gemini response could not be parsed as JSON.");

                    return MapToResult(payload, model);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < MaxAttempts)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        logger.LogWarning(ex,
                            "Gemini enrichment attempt {Attempt}/{MaxAttempts} with model {Model} failed for article {ArticleId}; retrying in {Delay}.",
                            attempt, MaxAttempts, model, article.Id, delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(ex,
                            "Gemini enrichment exhausted {MaxAttempts} attempts with model {Model} for article {ArticleId}; trying next fallback.",
                            MaxAttempts, model, article.Id);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"Gemini enrichment failed for article {article.Id} after exhausting all models.", lastException);
    }

    private static string BuildPrompt(Article article)
    {
        var content = article.RawContent.Length > MaxContentChars
            ? article.RawContent[..MaxContentChars]
            : article.RawContent;

        return $"""
            Analyze this news article for a financial market intelligence platform.
            Title: {article.Title}
            Published: {article.PublishedAt:u}
            Content:
            {content}
            """;
    }

    // response.Text comes back null both for transient SDK/network hiccups and for
    // content the model refused to answer (safety block, recitation, etc.) — surface
    // the actual reason so logs/RetryFailedEnrichmentJob sweeps are debuggable instead
    // of a generic "no text output" every time.
    private static string DescribeEmptyResponse(GenerateContentResponse response)
    {
        var blockReason = response.PromptFeedback?.BlockReason;
        if (blockReason is not null)
        {
            return $"Gemini blocked the prompt (blockReason={blockReason}).";
        }

        var finishReason = response.Candidates?.FirstOrDefault()?.FinishReason;
        if (finishReason is not null)
        {
            return $"Gemini returned no text (finishReason={finishReason}).";
        }

        return "Gemini response contained no text output.";
    }

    private void LogTokenUsage(Guid articleId, string model, GenerateContentResponse response)
    {
        var usage = response.UsageMetadata;
        if (usage is null)
        {
            return;
        }

        logger.LogInformation(
            "Gemini enrichment for article {ArticleId} with model {Model}: prompt={PromptTokens}, candidates={CandidateTokens}, total={TotalTokens} tokens.",
            articleId, model, usage.PromptTokenCount, usage.CandidatesTokenCount, usage.TotalTokenCount);
    }

    private static ArticleAnalysisResult MapToResult(GeminiAnalysisPayload payload, string modelVersion)
    {
        var sentiment = Enum.Parse<Sentiment>(payload.Sentiment, ignoreCase: true);
        var sectors = (payload.Sectors ?? [])
            .Select(s => new SectorImpactResult(
                s.Sector,
                Enum.Parse<ImpactDirection>(s.Direction, ignoreCase: true),
                s.Magnitude))
            .ToList();

        return new ArticleAnalysisResult(
            payload.Summary,
            payload.Category,
            sentiment,
            payload.SentimentConfidence,
            payload.ImpactScore,
            payload.ImpactRationale,
            sectors,
            payload.Keywords ?? [],
            modelVersion);
    }

    private static Schema BuildResponseSchema() => new()
    {
        Type = SchemaType.Object,
        Properties = new Dictionary<string, Schema>
        {
            ["summary"] = new Schema { Type = SchemaType.String, Description = "Neutral 2-3 sentence summary, no speculation." },
            ["category"] = new Schema
            {
                Type = SchemaType.String,
                Enum = ["Politics", "Economy", "Markets", "Banking", "Technology", "Commodities", "Crypto"]
            },
            ["sentiment"] = new Schema { Type = SchemaType.String, Enum = ["Bullish", "Bearish", "Neutral"] },
            ["sentimentConfidence"] = new Schema { Type = SchemaType.Number, Minimum = 0, Maximum = 1 },
            ["impactScore"] = new Schema
            {
                Type = SchemaType.Integer,
                Minimum = 0,
                Maximum = 100,
                Description = "Likely near-term market significance."
            },
            ["impactRationale"] = new Schema { Type = SchemaType.String },
            ["sectors"] = new Schema
            {
                Type = SchemaType.Array,
                Items = new Schema
                {
                    Type = SchemaType.Object,
                    Properties = new Dictionary<string, Schema>
                    {
                        ["sector"] = new Schema { Type = SchemaType.String },
                        ["direction"] = new Schema { Type = SchemaType.String, Enum = ["Positive", "Negative", "Neutral"] },
                        ["magnitude"] = new Schema { Type = SchemaType.Integer, Minimum = 0, Maximum = 100 }
                    },
                    Required = ["sector", "direction", "magnitude"]
                }
            },
            ["keywords"] = new Schema
            {
                Type = SchemaType.Array,
                Items = new Schema { Type = SchemaType.String },
                MaxItems = 8
            }
        },
        Required = ["summary", "category", "sentiment", "impactScore"]
    };

    private sealed record GeminiAnalysisPayload(
        string Summary,
        string Category,
        string Sentiment,
        float SentimentConfidence,
        int ImpactScore,
        string? ImpactRationale,
        List<GeminiSectorImpactPayload>? Sectors,
        List<string>? Keywords);

    private sealed record GeminiSectorImpactPayload(string Sector, string Direction, int Magnitude);
}
