namespace Arunika.Api.Contracts;

/// <summary>One sector on one day, from <c>GET /v1/sectors/history</c>.</summary>
public sealed record SectorHistoryPointDto(
    DateOnly Date,
    string Sector,
    int ArticleCount,
    int AverageImpactScore,
    int BullishCount,
    int BearishCount,
    int NeutralCount);
