namespace Arunika.Api.Contracts;

/// <summary>
/// <c>GET /v1/briefing</c> response. Simple V1 version per the to-do list:
/// top stories ranked by impact score for the day, no AI-generated executive
/// summary/overall sentiment/risk level yet — those arrive with
/// <c>GenerateDailyBriefingJob</c> (Phase 9).
/// </summary>
public sealed record BriefingResponseDto(DateOnly Date, IReadOnlyList<TopStoryDto> TopStories);

public sealed record TopStoryDto(Guid ArticleId, string Title, int ImpactScore, IReadOnlyList<string> Sectors);
