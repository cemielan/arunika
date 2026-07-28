namespace Arunika.Api.Contracts;

/// <summary>
/// <c>GET /v1/briefing</c> response. When <c>GenerateDailyBriefingJob</c>
/// (Phase 9) has run for the requested date, the AI-generated fields
/// (<see cref="ExecutiveSummary"/>, <see cref="OverallSentiment"/>,
/// <see cref="RiskLevel"/>) are populated; otherwise they fall back to a
/// computed-on-request market pulse. <see cref="TopStories"/> are always
/// computed from the same rolling <see cref="RangeStart"/>-<see cref="RangeEnd"/>
/// window.
/// </summary>
public sealed record BriefingResponseDto(
    DateOnly Date,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    int WindowDays,
    MarketPulseDto MarketPulse,
    IReadOnlyList<TopStoryDto> TopStories,
    string? ExecutiveSummary = null,
    string? OverallSentiment = null,
    string? RiskLevel = null);

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
