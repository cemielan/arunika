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

/// <summary>
/// One ranked story in the briefing. Carries the analysis fields the briefing
/// page needs to render an editorial excerpt — summary, sentiment, category,
/// source and publication time — all of which are already loaded alongside the
/// impact score, so exposing them costs no extra query.
/// </summary>
public sealed record TopStoryDto(
    Guid ArticleId,
    string Title,
    int ImpactScore,
    IReadOnlyList<string> Sectors,
    string? Summary = null,
    string? Sentiment = null,
    string? Category = null,
    string? Source = null,
    DateTimeOffset? PublishedAt = null,
    string? ImpactRationale = null);

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
