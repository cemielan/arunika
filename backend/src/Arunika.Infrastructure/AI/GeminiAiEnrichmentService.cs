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
///     Wraps the Gemini Developer API behind <see cref="IAiEnrichmentService"/>
///     (design doc §5): one structured-output call per article, combining
///     summary, classification, sentiment, impact score, sector impact, and
///     keywords, instead of six separate calls.
///     Uses proactive model rotation every 2 articles to distribute load
///     and avoid rate limits on free tier.
/// </summary>
public class GeminiAiEnrichmentService(
    IOptions<GeminiOptions> options,
    GeminiRateLimiter rateLimiter,
    GeminiCircuitBreaker circuitBreaker,
    GeminiModelRotator modelRotator,
    ILogger<GeminiAiEnrichmentService> logger)
    : IAiEnrichmentService
{
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
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var client = new Client(apiKey: options.Value.ApiKey);
        var prompt = BuildPrompt(article);
        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = ResponseSchema,
            SafetySettings = SafetySettings
        };

        var maxAttempts = Math.Max(1, options.Value.MaxAttemptsPerModel);
        var candidates = modelRotator.GetCandidates();

        if (!rateLimiter.AnyAvailable(candidates))
        {
            // Fail fast rather than walk the chain: every model is either
            // parked or out of daily budget, so the composite service should
            // reach the next provider now, and RetryFailedEnrichmentJob can
            // pick the article up once a rolling window frees.
            throw new GeminiModelExhaustedException(candidates[0],
                $"Every Gemini model is out of budget; skipping article {article.Id}.");
        }

        Exception? lastException = null;

        foreach (var model in candidates)
        {
            if (!circuitBreaker.IsAvailable(model))
            {
                logger.LogDebug("Gemini circuit breaker open for model {Model}; skipping.", model);
                continue;
            }

            if (!rateLimiter.IsAvailable(model))
            {
                logger.LogDebug("Gemini model {Model} has no budget left; skipping.", model);
                continue;
            }

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await rateLimiter.WaitForSlotAsync(model, cancellationToken);

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

                    modelRotator.RecordSuccess(model);
                    circuitBreaker.RecordSuccess(model);
                    return MapToResult(payload, model);
                }
                catch (GeminiModelExhaustedException ex)
                {
                    // Budget, not fault: don't retry it and don't hold it
                    // against the model's health — just move down the chain.
                    lastException = ex;
                    logger.LogDebug("Gemini model {Model} unavailable for article {ArticleId}: {Reason}",
                        model, article.Id, ex.Message);
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    modelRotator.RecordFailure(model);
                    circuitBreaker.RecordFailure(model);

                    if (IsQuotaExceeded(ex))
                    {
                        // Park this model only. A shared cooldown used to take
                        // the entire Gemini provider offline whenever one of
                        // the 20-RPD Flash models ran dry.
                        rateLimiter.MarkModelCooldown(model, options.Value.ModelQuotaCooldown);
                        logger.LogWarning(
                            "Gemini reported a quota breach on model {Model} for article {ArticleId}; trying the next model.",
                            model, article.Id);
                        break;
                    }

                    if (!IsRetryable(ex) || attempt == maxAttempts)
                    {
                        logger.LogWarning(ex,
                            "Gemini enrichment failed on model {Model} for article {ArticleId} after {Attempt} attempt(s); trying the next model.",
                            model, article.Id, attempt);
                        break;
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    logger.LogWarning(ex,
                        "Gemini enrichment attempt {Attempt}/{MaxAttempts} with model {Model} failed for article {ArticleId}; retrying in {Delay}.",
                        attempt, maxAttempts, model, article.Id, delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException(
            $"Gemini enrichment failed for article {article.Id} after exhausting all models.", lastException);
    }

    /// <summary>
    /// Only genuinely transient faults are worth a second attempt. Retrying a
    /// refusal, a malformed response, or an unsupported model just burns the
    /// model's daily quota and starves the articles behind it in the queue.
    /// </summary>
    private static bool IsRetryable(Exception ex)
    {
        if (IsUnsupportedModel(ex) || IsQuotaExceeded(ex))
        {
            return false;
        }

        // A schema violation is a property of the prompt, not of the moment.
        if (ex is JsonException)
        {
            return false;
        }

        // A dropped connection or a timeout usually clears on its own.
        if (ex is HttpRequestException or TaskCanceledException)
        {
            return true;
        }

        var message = ex.ToString();

        // Content the model declined to answer comes back as an empty response
        // and will come back empty again on a retry.
        if (message.Contains("blockReason=", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("finishReason=SAFETY", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("finishReason=RECITATION", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("API key not valid", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("PERMISSION_DENIED", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsQuotaExceeded(Exception ex)
    {
        var message = ex.ToString();

        return message.Contains("Quota exceeded for metric", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("You exceeded your current quota", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnsupportedModel(Exception ex)
    {
        var message = ex.ToString();

        return message.Contains("is not found for API version", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("is not supported for generateContent", StringComparison.OrdinalIgnoreCase);
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
