using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.AI;

public class OpenRouterAiEnrichmentService(
    IOptions<OpenRouterOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenRouterAiEnrichmentService> logger) : IAiEnrichmentService
{
    private const int MaxAttempts = 3;
    private const int MaxContentChars = 6000;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<ArticleAnalysisResult> AnalyzeAsync(Article article, CancellationToken cancellationToken = default)
    {
        var modelsToTry = new List<string> { options.Value.Model };
        modelsToTry.AddRange(options.Value.FallbackModels);

        Exception? lastException = null;

        foreach (var model in modelsToTry)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    var result = await CallOpenRouterAsync(article, model, cancellationToken);
                    logger.LogInformation(
                        "OpenRouter enrichment succeeded for article {ArticleId} with model {Model}.",
                        article.Id, model);
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < MaxAttempts)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        logger.LogWarning(ex,
                            "OpenRouter enrichment attempt {Attempt}/{MaxAttempts} with model {Model} failed for article {ArticleId}; retrying in {Delay}.",
                            attempt, MaxAttempts, model, article.Id, delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(ex,
                            "OpenRouter enrichment exhausted {MaxAttempts} attempts with model {Model} for article {ArticleId}; trying next fallback.",
                            MaxAttempts, model, article.Id);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"OpenRouter enrichment failed for article {article.Id} after exhausting all models.", lastException);
    }

    private async Task<ArticleAnalysisResult> CallOpenRouterAsync(Article article, string model, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("openrouter");
        var content = article.RawContent.Length > MaxContentChars
            ? article.RawContent[..MaxContentChars]
            : article.RawContent;

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = """
You are a financial market intelligence analyst. Analyze the news article and return a JSON object with:
- summary: neutral 2-3 sentence summary, no speculation
- category: one of "Politics", "Economy", "Markets", "Banking", "Technology", "Commodities", "Crypto"
- sentiment: "Bullish", "Bearish", or "Neutral"
- sentimentConfidence: number 0-1
- impactScore: integer 0-100 (likely near-term market significance)
- impactRationale: string explaining the score
- sectors: array of { sector: string, direction: "Positive"|"Negative"|"Neutral", magnitude: integer 0-100 }
- keywords: array of strings, max 8 items

Return ONLY valid JSON, no markdown, no explanation.
"""
                },
                new
                {
                    role = "user",
                    content = $"Title: {article.Title}\nPublished: {article.PublishedAt:u}\nContent:\n{content}"
                }
            },
            response_format = new { type = "json_object" }
        };

        var httpResponse = await client.PostAsJsonAsync("chat/completions", requestBody, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseBody = await httpResponse.Content.ReadFromJsonAsync<OpenRouterResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("OpenRouter returned empty response.");

        var choice = responseBody.Choices?.FirstOrDefault()
            ?? throw new InvalidOperationException("OpenRouter returned no choices.");

        var text = choice.Message?.Content
            ?? throw new InvalidOperationException("OpenRouter returned no content in message.");

        var payload = JsonSerializer.Deserialize<OpenRouterAnalysisPayload>(text, JsonOptions)
            ?? throw new InvalidOperationException("OpenRouter response could not be parsed as JSON.");

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
            $"openrouter/{model}");
    }

    private sealed record OpenRouterResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("choices")] List<OpenRouterChoice>? Choices,
        [property: JsonPropertyName("usage")] OpenRouterUsage? Usage);

    private sealed record OpenRouterChoice(
        [property: JsonPropertyName("message")] OpenRouterMessage? Message,
        [property: JsonPropertyName("finish_reason")] string? FinishReason);

    private sealed record OpenRouterMessage(
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("role")] string? Role);

    private sealed record OpenRouterUsage(
        [property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int? CompletionTokens,
        [property: JsonPropertyName("total_tokens")] int? TotalTokens);

    private sealed record OpenRouterAnalysisPayload(
        string Summary,
        string Category,
        string Sentiment,
        float SentimentConfidence,
        int ImpactScore,
        string? ImpactRationale,
        List<OpenRouterSectorImpactPayload>? Sectors,
        List<string>? Keywords);

    private sealed record OpenRouterSectorImpactPayload(string Sector, string Direction, int Magnitude);
}
