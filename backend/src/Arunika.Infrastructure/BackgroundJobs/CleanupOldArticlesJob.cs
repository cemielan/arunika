using Arunika.Application.Abstractions;
using Arunika.Application.Constants;
using Arunika.Application.Services;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.BackgroundJobs;

public class CleanupOldArticlesJob(
    IArticleRepository articleRepository,
    ISectorStatsRepository sectorStatsRepository,
    IOptions<RetentionOptions> options,
    ILogger<CleanupOldArticlesJob> logger)
{
    // The briefing and sector windows are 7 days wide; keeping less would empty them.
    private const int MinimumRetentionDays = 7;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var retentionDays = Math.Max(MinimumRetentionDays, options.Value.ArticleDays);

        // Snapshot first: once the articles are gone, their days can't be counted.
        await SnapshotSectorStatsAsync(retentionDays, cancellationToken);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var deleted = await articleRepository.DeleteOlderThanAsync(cutoff, cancellationToken);

        logger.LogInformation(
            "CleanupOldArticlesJob: deleted {Count} article(s) published before {Cutoff}.",
            deleted, cutoff);
    }

    /// <summary>
    /// Recounts every complete day still inside the retention window rather
    /// than only yesterday. Each day is then counted one last time on the night
    /// its articles start to be deleted, by which point late enrichment from
    /// RetryFailedEnrichmentJob has landed, and a missed night is caught up.
    /// </summary>
    private async Task SnapshotSectorStatsAsync(int retentionDays, CancellationToken cancellationToken)
    {
        var jakarta = JakartaTimeZone();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, jakarta).DateTime);
        var firstDay = today.AddDays(-retentionDays);
        var lastDay = today.AddDays(-1);

        var articles = await articleRepository.GetEnrichedArticlesInRangeAsync(
            StartOfDayUtc(firstDay, jakarta), StartOfDayUtc(today, jakarta), cancellationToken);

        var stats = articles
            .GroupBy(a => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(a.PublishedAt, jakarta).DateTime))
            .SelectMany(day => SentimentTally.BySector(day).Select(sector => new SectorDailyStat
            {
                Date = day.Key,
                SectorId = sector.SectorId,
                ArticleCount = sector.Tally.Total,
                BullishCount = sector.Tally.BullishCount,
                BearishCount = sector.Tally.BearishCount,
                NeutralCount = sector.Tally.NeutralCount,
                AverageImpactScore = sector.Tally.AverageImpactScore
            }))
            .ToList();

        await sectorStatsRepository.ReplaceRangeAsync(firstDay, lastDay, stats, cancellationToken);

        logger.LogInformation(
            "CleanupOldArticlesJob: stored {Count} sector stat row(s) for {From} to {To}.",
            stats.Count, firstDay, lastDay);
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly day, TimeZoneInfo zone)
    {
        var local = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
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
}
