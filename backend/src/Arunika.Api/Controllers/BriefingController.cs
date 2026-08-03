using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Arunika.Application.Services;
using Arunika.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

/// <summary>
/// <c>GET /v1/briefing</c> — daily morning brief (design doc §7). Returns the
/// cached AI-generated briefing (executive summary, sentiment, risk level) when
/// <c>GenerateDailyBriefingJob</c> has already run for the requested date;
/// otherwise generates it on demand and falls back to the most recent briefing
/// only when generation fails. Top stories and market pulse are always computed
/// from the same rolling <see cref="WindowDays"/>-day window.
/// </summary>
[ApiController]
[Route("v1/briefing")]
[Produces("application/json")]
public class BriefingController(
    IArticleRepository articleRepository,
    IArticleAnalysisRepository articleAnalysisRepository,
    DailyBriefingService dailyBriefingService,
    ILogger<BriefingController> logger) : ControllerBase
{
    private const int TopStoryCount = 10;
    private const int WindowDays = 7;

    /// <summary>
    /// Returns the top stories and overall market pulse for the rolling <see cref="WindowDays"/>-day
    /// window ending on the given date (defaults to today, UTC). Includes the AI-generated
    /// executive summary/sentiment/risk level if <c>GenerateDailyBriefingJob</c> has run.
    /// </summary>
    /// <param name="date">The last day of the window. Defaults to today (UTC).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The briefing for the requested window.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BriefingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBriefing([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var todayJakarta = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, JakartaTimeZone()).DateTime);
        var rangeEnd = date ?? todayJakarta;
        var rangeStart = rangeEnd.AddDays(-(WindowDays - 1));
        var (from, to) = ToUtcRange(rangeStart, rangeEnd);

        // Try the cached briefing for the requested date first.
        var cached = await articleAnalysisRepository.GetBriefingByDateAsync(rangeEnd, cancellationToken);

        // If today's briefing hasn't been generated yet (e.g. the 06:00 job was
        // skipped because the host was asleep, or it failed), generate it on
        // demand so the executive summary refreshes every day. Only for "today"
        // to avoid expensive AI calls for historical dates.
        if (cached is null && rangeEnd == todayJakarta)
        {
            try
            {
                cached = await dailyBriefingService.GenerateForDateAsync(rangeEnd, cancellationToken);
            }
            catch (Exception ex)
            {
                // AI provider down / rate-limited: fall through to the most
                // recent briefing rather than failing the whole request.
                logger.LogWarning(ex, "On-demand briefing generation failed; serving the most recent briefing.");
            }
        }

        // If no briefing exists for the requested date, fall back to the most
        // recent briefing.
        if (cached is null)
        {
            cached = await articleAnalysisRepository.GetLatestBriefingAsync(cancellationToken);
        }

        var articles = await articleRepository.GetEnrichedArticlesInRangeAsync(from, to, cancellationToken);

        var topStories = articles
            .OrderByDescending(article => article.Analysis!.ImpactScore)
            .Take(TopStoryCount)
            .Select(article => new TopStoryDto(
                article.Id,
                article.Title,
                article.Analysis!.ImpactScore,
                article.SectorImpacts.Select(impact => impact.Sector?.Name ?? string.Empty).ToList()))
            .ToList();

        var marketPulse = BuildMarketPulse(articles);

        var data = new BriefingResponseDto(
            rangeEnd, rangeStart, rangeEnd, WindowDays, marketPulse, topStories,
            cached?.ExecutiveSummary,
            cached is not null ? cached.OverallSentiment.ToString() : null,
            cached is not null ? cached.RiskLevel.ToString() : null);
        var meta = new { generatedAt = DateTimeOffset.UtcNow };

        return Ok(new ApiResponse<BriefingResponseDto>(data, meta));
    }

    private static (DateTimeOffset From, DateTimeOffset To) ToUtcRange(DateOnly rangeStart, DateOnly rangeEnd)
    {
        var jakarta = JakartaTimeZone();
        var startLocal = DateTime.SpecifyKind(rangeStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endExclusiveLocal = DateTime.SpecifyKind(rangeEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

        var from = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, jakarta), TimeSpan.Zero);
        var to = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endExclusiveLocal, jakarta), TimeSpan.Zero);
        return (from, to);
    }

    private static TimeZoneInfo JakartaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static MarketPulseDto BuildMarketPulse(IReadOnlyList<Arunika.Domain.Entities.Article> articles)
    {
        var bullish = 0;
        var bearish = 0;
        var neutral = 0;
        var impactSum = 0;

        foreach (var article in articles)
        {
            impactSum += article.Analysis!.ImpactScore;
            switch (article.Analysis!.Sentiment)
            {
                case Sentiment.Bullish:
                    bullish++;
                    break;
                case Sentiment.Bearish:
                    bearish++;
                    break;
                default:
                    neutral++;
                    break;
            }
        }

        var total = articles.Count;
        var averageImpact = total > 0 ? (int)Math.Round(impactSum / (double)total) : 0;

        var sentiment = "Neutral";
        if (bullish > bearish && bullish > neutral)
        {
            sentiment = "Bullish";
        }
        else if (bearish > bullish && bearish > neutral)
        {
            sentiment = "Bearish";
        }

        var dominant = Math.Max(bullish, Math.Max(bearish, neutral));
        var confidence = total > 0 ? (int)Math.Round(dominant / (double)total * 100) : 0;

        return new MarketPulseDto(sentiment, bullish, bearish, neutral, total, averageImpact, confidence);
    }
}
