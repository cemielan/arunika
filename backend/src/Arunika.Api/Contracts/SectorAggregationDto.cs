namespace Arunika.Api.Contracts;

public sealed record SectorAggregationDto(
    string Sector,
    int ArticleCount,
    int AverageImpactScore,
    int BullishCount,
    int BearishCount,
    int NeutralCount,
    string DominantSentiment);
