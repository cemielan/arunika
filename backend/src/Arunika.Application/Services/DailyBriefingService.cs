using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;

namespace Arunika.Application.Services;

/// <summary>
/// Generates and persists the daily briefing (executive summary, sentiment,
/// risk level, top stories) for a given date. Shared by GenerateDailyBriefingJob
/// (pre-generates at 06:00) and the briefing endpoint's on-demand fallback, so
/// the AI summary refreshes every day even when the scheduled job never ran
/// (e.g. the host was asleep at 06:00).
/// </summary>
public class DailyBriefingService(
    IArticleRepository articleRepository,
    IArticleAnalysisRepository articleAnalysisRepository,
    IBriefingGenerationService briefingGenerationService)
{
    private const int TopStoryCount = 10;
    private const int WindowDays = 7;

    /// <summary>
    /// Generates and saves the briefing for <paramref name="date"/>. Returns the
    /// saved briefing, or null when there are no enriched articles for that date.
    /// </summary>
    public async Task<Briefing?> GenerateForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var rangeStart = date.AddDays(-(WindowDays - 1));
        var (from, to) = ToUtcRange(rangeStart, date);

        var topStories = (await articleRepository.GetEnrichedArticlesInRangeAsync(from, to, cancellationToken))
            .OrderByDescending(article => article.Analysis!.ImpactScore)
            .Take(TopStoryCount)
            .ToList();

        if (topStories.Count == 0)
        {
            return null;
        }

        var result = await briefingGenerationService.GenerateAsync(topStories, cancellationToken);

        var briefing = new Briefing
        {
            Id = Guid.NewGuid(),
            BriefingDate = date,
            ExecutiveSummary = result.ExecutiveSummary,
            OverallSentiment = result.OverallSentiment,
            RiskLevel = result.RiskLevel,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        for (var i = 0; i < topStories.Count; i++)
        {
            briefing.Items.Add(new BriefingItem
            {
                BriefingId = briefing.Id,
                ArticleId = topStories[i].Id,
                Rank = i + 1
            });
        }

        await articleAnalysisRepository.SaveBriefingAsync(briefing, cancellationToken);
        return briefing;
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
}
