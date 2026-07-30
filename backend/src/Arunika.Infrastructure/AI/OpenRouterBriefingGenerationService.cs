using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.AI;

public class OpenRouterBriefingGenerationService(
    IOptions<OpenRouterOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenRouterBriefingGenerationService> logger) : IBriefingGenerationService
{
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<BriefingGenerationResult> GenerateAsync(IReadOnlyList<Article> topStories, CancellationToken cancellationToken = default)
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
                    var result = await CallOpenRouterAsync(topStories, model, cancellationToken);
                    logger.LogInformation(
                        "OpenRouter briefing generation succeeded with model {Model}.", model);
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < MaxAttempts)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        logger.LogWarning(ex,
                            "OpenRouter briefing generation attempt {Attempt}/{MaxAttempts} with model {Model} failed; retrying in {Delay}.",
                            attempt, MaxAttempts, model, delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(ex,
                            "OpenRouter briefing generation exhausted {MaxAttempts} attempts with model {Model}; trying next fallback.",
                            MaxAttempts, model);
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenRouter briefing generation failed after exhausting all models.", lastException);
    }

    private async Task<BriefingGenerationResult> CallOpenRouterAsync(IReadOnlyList<Article> topStories, string model, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("openrouter");
        var storiesText = string.Join("\n\n", topStories.Select((a, i) =>
            $"Story {i + 1}: {a.Title}\nSummary: {a.Analysis?.Summary ?? "N/A"}\nCategory: {a.Analysis?.Category?.Name ?? "N/A"}\nImpact Score: {a.Analysis?.ImpactScore ?? 0}\nSentiment: {a.Analysis?.Sentiment}"));

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = """
You are a financial market intelligence analyst. Below are the top market news stories for today,
ranked by market impact. Generate a concise daily briefing in JSON format.

Return a JSON object with:
- executiveSummary: 2-3 paragraph executive summary synthesizing the key themes
- overallSentiment: "Bullish", "Bearish", or "Neutral"
- riskLevel: "Low", "Medium", or "High"

Return ONLY valid JSON, no markdown, no explanation.
"""
                },
                new
                {
                    role = "user",
                    content = storiesText
                }
            },
            response_format = new { type = "json_object" }
        };

        var httpResponse = await client.PostAsJsonAsync("chat/completions", requestBody, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseBody = await httpResponse.Content.ReadFromJsonAsync<OpenRouterBriefingResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("OpenRouter returned empty response for briefing.");

        var choice = responseBody.Choices?.FirstOrDefault()
            ?? throw new InvalidOperationException("OpenRouter returned no choices for briefing.");

        var text = choice.Message?.Content
            ?? throw new InvalidOperationException("OpenRouter returned no content in message for briefing.");

        var payload = JsonSerializer.Deserialize<OpenRouterBriefingPayload>(text, JsonOptions)
            ?? throw new InvalidOperationException("OpenRouter briefing response could not be parsed.");

        return new BriefingGenerationResult(
            payload.ExecutiveSummary,
            Enum.Parse<Sentiment>(payload.OverallSentiment, ignoreCase: true),
            Enum.Parse<RiskLevel>(payload.RiskLevel, ignoreCase: true));
    }

    private sealed record OpenRouterBriefingResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("choices")] List<OpenRouterBriefingChoice>? Choices);

    private sealed record OpenRouterBriefingChoice(
        [property: JsonPropertyName("message")] OpenRouterBriefingMessage? Message);

    private sealed record OpenRouterBriefingMessage(
        [property: JsonPropertyName("content")] string? Content);

    private sealed record OpenRouterBriefingPayload(
        [property: JsonPropertyName("executiveSummary")] string ExecutiveSummary,
        [property: JsonPropertyName("overallSentiment")] string OverallSentiment,
        [property: JsonPropertyName("riskLevel")] string RiskLevel);
}
