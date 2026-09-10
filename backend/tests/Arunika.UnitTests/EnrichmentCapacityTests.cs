using Arunika.Infrastructure;
using Arunika.Infrastructure.AI;

namespace Arunika.UnitTests;

/// <summary>
/// Capacity planning for the free-tier Gemini budget against the RSS feeds
/// registered in <see cref="DependencyInjection.RssFeeds"/>.
///
/// GeminiQuotaTests covers the limiter's mechanics — that it derates, budgets
/// per model, and throws instead of overspending. This file covers the question
/// the limiter cannot answer on its own: whether the budget is actually large
/// enough for the feeds we poll, so adding a feed or lowering
/// QuotaSafetyPercent fails the build instead of quietly producing an
/// enrichment backlog.
///
/// Volumes below were measured on 2026-09-11 by counting items with a pubDate
/// inside the last 24 hours, per feed:
///
///   Federal Reserve Press Releases    0   (press releases, days apart)
///   WSJ Markets                      30
///   BBC Business                     15
///   Antara Ekonomi                    0   (feed lags; 20 items, oldest 400h)
///   CNBC                             30   (whole feed is &lt; 24h old)
///   MarketWatch                      10   (whole feed is &lt; 4h old)
///   CNBC Indonesia Market            35
///   Kontan Investasi                 25
///   Detik Finance                    56
///                                   ---
///   Total                           201 new articles/day
///   Cold start (all 420 feed items enrich once on the first run)
/// </summary>
public class EnrichmentCapacityTests
{
    /// <summary>Measured new articles per day across the registered feeds.</summary>
    private const int MeasuredDailyArticles = 201;

    /// <summary>
    /// Items present across all registered feeds at one moment. The first
    /// FetchNewsJob run after a deploy sees every one of them as new, so this
    /// is the one-off peak the budget has to absorb.
    /// </summary>
    private const int ColdStartArticles = 420;

    /// <summary>GenerateDailyBriefingJob runs on "0 */6 * * *" — four Gemini calls a day.</summary>
    private const int DailyBriefingCalls = 4;

    /// <summary>
    /// Enrichment is one GenerateContentAsync call per article on the happy
    /// path: GeminiAiEnrichmentService asks for every field in a single
    /// structured-JSON response rather than one call per field.
    /// </summary>
    private const int RequestsPerArticle = 1;

    private static readonly GeminiOptions Options = new();

    /// <summary>
    /// Mirrors GeminiRateLimiter's own arithmetic: each model's published RPD
    /// derated by QuotaSafetyPercent, summed over the configured chain.
    /// </summary>
    private static int EffectiveDailyBudget()
    {
        var percent = Math.Clamp(Options.QuotaSafetyPercent, 1, 100);
        var chain = new List<string> { Options.Model };
        chain.AddRange(Options.FallbackModels);

        return chain
            .Distinct()
            .Sum(model =>
            {
                var rpd = Options.ModelQuotas.TryGetValue(model, out var quota) ? quota.Rpd : 20;
                return Math.Max(1, rpd * percent / 100);
            });
    }

    [Fact]
    public void SteadyStateDemand_FitsInTheDeratedDailyBudget_WithRoomToSpare()
    {
        var demand = (MeasuredDailyArticles * RequestsPerArticle) + DailyBriefingCalls;
        var budget = EffectiveDailyBudget();

        // Half the budget left over absorbs re-enrichment sweeps, retries on
        // transient failures, and a busy news day well above the measured mean.
        Assert.True(demand <= budget / 2,
            $"Daily enrichment demand ({demand}) should stay under half the derated Gemini budget ({budget}). "
            + "Either trim DependencyInjection.RssFeeds or raise the quota before shipping.");
    }

    [Fact]
    public void ColdStartBurst_FitsInASingleDayOfBudget()
    {
        var demand = (ColdStartArticles * RequestsPerArticle) + DailyBriefingCalls;
        var budget = EffectiveDailyBudget();

        Assert.True(demand <= budget,
            $"The first FetchNewsJob run enqueues {demand} enrichments, which exceeds the derated "
            + $"daily Gemini budget ({budget}); the overflow would be marked Failed and deferred to "
            + "RetryFailedEnrichmentJob for a day.");
    }

    [Fact]
    public void PerMinuteCeiling_DrainsAFetchCycleWellInsideTheNextOne()
    {
        // FetchNewsJob runs on "*/30 * * * *" — 48 cycles a day.
        const int fetchCyclesPerDay = 48;
        var articlesPerCycle = (int)Math.Ceiling(MeasuredDailyArticles / (double)fetchCyclesPerDay);

        var minutesToDrain = articlesPerCycle / (double)Math.Max(1, Options.MaxRequestsPerMinute);

        Assert.True(minutesToDrain < 30,
            $"A fetch cycle enqueues about {articlesPerCycle} articles, which needs "
            + $"{minutesToDrain:0.0} min at {Options.MaxRequestsPerMinute} requests/min — longer than the "
            + "30 min until the next cycle, so the backlog would grow without bound.");
    }

    [Fact]
    public void EveryArticleInAFetchCycle_GetsASlotInsideMaxSlotWait()
    {
        // Two Hangfire enrichment workers share the global per-minute ceiling,
        // so a worker waits roughly (workers / RPM) minutes for its slot. If
        // that exceeds MaxSlotWait the limiter reports the model exhausted and
        // the article is marked Failed even though budget remained.
        const int enrichmentWorkers = 2;

        var secondsPerSlot = 60.0 / Math.Max(1, Options.MaxRequestsPerMinute);
        var worstCaseWait = TimeSpan.FromSeconds(secondsPerSlot * enrichmentWorkers);

        Assert.True(worstCaseWait < Options.MaxSlotWait,
            $"A worker waits up to {worstCaseWait.TotalSeconds:0}s for a slot at "
            + $"{Options.MaxRequestsPerMinute} requests/min, but MaxSlotWait is "
            + $"{Options.MaxSlotWait.TotalSeconds:0}s — articles would fail on the wait, not on quota.");
    }
}
