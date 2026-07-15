namespace Arunika.Api.Contracts;

/// <summary>
/// <c>GET /v1/briefing</c> response. Simple V1 version per the to-do list:
/// top stories ranked by impact score for the day, no AI-generated executive
/// summary/overall sentiment/risk level yet — those arrive with
/// <c>GenerateDailyBriefingJob</c> (Phase 9). <see cref="TopStories"/> and
/// <see cref="MarketPulse"/> are both computed from the same rolling
/// <see cref="RangeStart"/>-<see cref="RangeEnd"/> window so the two never
/// disagree about "recent".
/// </summary>
public sealed record BriefingResponseDto(
    DateOnly Date,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    int WindowDays,
    MarketPulseDto MarketPulse,
    IReadOnlyList<TopStoryDto> TopStories);

public sealed record TopStoryDto(Guid ArticleId, string Title, int ImpactScore, IReadOnlyList<string> Sectors);

/// <summary>
/// Aggregate sentiment/impact conclusion across every enriched, non-duplicate
/// article published within the briefing's rolling window.
/// </summary>
public sealed record MarketPulseDto(
    string Sentiment,
    int BullishCount,
    int BearishCount,
    int NeutralCount,
    int TotalArticles,
    int AverageImpactScore,
    int Confidence);
