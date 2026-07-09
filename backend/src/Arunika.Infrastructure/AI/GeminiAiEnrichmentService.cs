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
public class GeminiAiEnrichmentService(IOptions<GeminiOptions> options, ILogger<GeminiAiEnrichmentService> logger)
    : IAiEnrichmentService
{
    private const int MaxAttempts = 3;
    private const int MaxContentChars = 6000;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Schema ResponseSchema = BuildResponseSchema();

    public async Task<ArticleAnalysisResult> AnalyzeAsync(Article article, CancellationToken cancellationToken = default)
    {
        var client = new Client(apiKey: options.Value.ApiKey);
        var prompt = BuildPrompt(article);
        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = ResponseSchema
        };

        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var response = await client.Models.GenerateContentAsync(
                    model: options.Value.Model,
                    contents: prompt,
                    config: config,
                    cancellationToken: cancellationToken);

                LogTokenUsage(article.Id, response);

                var text = response.Text
                    ?? throw new InvalidOperationException("Gemini response contained no text output.");
                var payload = JsonSerializer.Deserialize<GeminiAnalysisPayload>(text, JsonOptions)
                    ?? throw new InvalidOperationException("Gemini response could not be parsed as JSON.");

                return MapToResult(payload, options.Value.Model);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt == MaxAttempts)
                {
                    break;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.LogWarning(ex,
                    "Gemini enrichment attempt {Attempt}/{MaxAttempts} failed for article {ArticleId}; retrying in {Delay}.",
                    attempt, MaxAttempts, article.Id, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Gemini enrichment failed for article {article.Id} after {MaxAttempts} attempts.", lastException);
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

    private void LogTokenUsage(Guid articleId, GenerateContentResponse response)
    {
        var usage = response.UsageMetadata;
        if (usage is null)
        {
            return;
        }

        logger.LogInformation(
            "Gemini enrichment for article {ArticleId}: prompt={PromptTokens}, candidates={CandidateTokens}, total={TotalTokens} tokens.",
            articleId, usage.PromptTokenCount, usage.CandidatesTokenCount, usage.TotalTokenCount);
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
