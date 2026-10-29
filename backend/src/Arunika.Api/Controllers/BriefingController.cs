using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Arunika.Application.Constants;
using Arunika.Application.Services;
using Arunika.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Arunika.Api.Controllers;

/// <summary>
/// <c>GET /v1/briefing</c> — daily morning brief (design doc §7). Returns the
/// cached AI-generated briefing (executive summary, sentiment, risk level) when
/// <c>GenerateDailyBriefingJob</c> has already run for the requested date;
/// otherwise generates it on demand and falls back to the most recent briefing
/// only when generation fails. Top stories and market pulse are computed from
/// the same rolling <see cref="WindowDays"/>-day window, except for a past date
/// with its own briefing, which serves the ranking and pulse stored with it.
/// </summary>
[ApiController]
[Route("v1/briefing")]
[Produces("application/json")]
// Tighter than the global limit: a cache miss here can start an AI generation.
[EnableRateLimiting(RateLimiting.BriefingPolicy)]
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
                // The AI providers retry multiple models with backoff (and may
                // wait for a free-tier rate-limit slot), which can take minutes.
                // The briefing page must not hang on that — cap on-demand
                // generation at a short budget and serve the most recent
                // briefing instead, so the endpoint always responds promptly.
                using var generationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                generationCts.CancelAfter(TimeSpan.FromSeconds(20));
                cached = await dailyBriefingService.GenerateForDateAsync(rangeEnd, generationCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("On-demand briefing generation exceeded its time budget; serving the most recent briefing.");
            }
            catch (Exception ex)
            {
                // AI provider down / rate-limited: fall through to the most
                // recent briefing rather than failing the whole request.
                logger.LogWarning(ex, "On-demand briefing generation failed; serving the most recent briefing.");
            }
        }

        // A past date with its own briefing is the archive: serve the stories and
        // pulse stored with it. Its window's articles have mostly been deleted by
        // now, so recounting them would show a fraction of what was published.
        var isArchive = rangeEnd < todayJakarta && cached is { Items.Count: > 0 };

        // Without a briefing of its own, today borrows the most recent one. A
        // past date doesn't: that would put another day's summary under it.
        if (cached is null && rangeEnd >= todayJakarta)
        {
            cached = await articleAnalysisRepository.GetLatestBriefingAsync(cancellationToken);
        }

        List<Article> storyArticles;
        MarketPulseDto marketPulse;

        if (isArchive)
        {
            storyArticles = cached!.Items
                .OrderBy(item => item.Rank)
                .Select(item => item.Article)
                .Where(article => article?.Analysis is not null)
                .Select(article => article!)
                .ToList();

            var stored = new SentimentTally(cached.BullishCount, cached.BearishCount, cached.NeutralCount, cached.AverageImpactScore);
            // Briefings saved before the pulse was stored have no counts; a
            // partial recount beats an empty pulse.
            marketPulse = stored.Total > 0
                ? BuildMarketPulse(stored)
                : BuildMarketPulse(SentimentTally.From(await articleRepository.GetEnrichedArticlesInRangeAsync(from, to, cancellationToken)));
        }
        else
        {
            var articles = await articleRepository.GetEnrichedArticlesInRangeAsync(from, to, cancellationToken);

            // Repository already returns articles in ranking order.
            storyArticles = TopStorySelector.Select(articles, TopStoryCount);

            marketPulse = BuildMarketPulse(SentimentTally.From(articles));
        }

        var sourceCounts = await articleRepository.GetSourceCountsAsync(
            storyArticles.Select(article => article.Id).ToList(), cancellationToken);

        var topStories = storyArticles
            .Select(article => new TopStoryDto(
                article.Id,
                article.Title,
                article.Analysis!.ImpactScore,
                article.SectorImpacts
                    .Select(impact => impact.Sector?.Name ?? string.Empty)
                    .Where(name => name.Length > 0)
                    .ToList(),
                article.Analysis!.Summary,
                article.Analysis!.Sentiment.ToString(),
                article.Analysis!.Category?.Name,
                article.Source?.Name,
                article.PublishedAt,
                article.Analysis!.ImpactRationale,
                sourceCounts.GetValueOrDefault(article.Id, 1)))
            .ToList();

        var (previousDate, nextDate) = await articleAnalysisRepository.GetAdjacentBriefingDatesAsync(rangeEnd, cancellationToken);

        var data = new BriefingResponseDto(
            rangeEnd, rangeStart, rangeEnd, WindowDays, marketPulse, topStories,
            cached?.ExecutiveSummary,
            cached is not null ? cached.OverallSentiment.ToString() : null,
            cached is not null ? cached.RiskLevel.ToString() : null,
            isArchive,
            previousDate,
            // Today has no "next": anything later is a future date.
            rangeEnd >= todayJakarta ? null : nextDate);
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
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConstants.JakartaIana);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConstants.JakartaWindows);
        }
    }

    private static MarketPulseDto BuildMarketPulse(SentimentTally tally)
    {
        var total = tally.Total;
        var dominant = Math.Max(tally.BullishCount, Math.Max(tally.BearishCount, tally.NeutralCount));
        var confidence = total > 0 ? (int)Math.Round(dominant / (double)total * 100) : 0;

        return new MarketPulseDto(
            tally.Dominant.ToString(), tally.BullishCount, tally.BearishCount, tally.NeutralCount,
            total, tally.AverageImpactScore, confidence);
    }
}
