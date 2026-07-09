namespace Arunika.Api.Contracts;

/// <summary>
/// Full detail payload for <c>GET /v1/articles/{id}</c> (design doc §7).
/// </summary>
public sealed record ArticleDetailDto(
    Guid Id,
    string Title,
    string Url,
    string Source,
    DateTimeOffset PublishedAt,
    string EnrichmentStatus,
    string? Summary,
    string? Category,
    string? Sentiment,
    float? SentimentConfidence,
    int? ImpactScore,
    string? ImpactRationale,
    IReadOnlyList<SectorImpactDto> Sectors,
    IReadOnlyList<string> Keywords,
    Guid? DuplicateOfId,
    IReadOnlyList<DuplicateArticleDto> Duplicates);

public sealed record SectorImpactDto(string Sector, string Direction, int Magnitude);

public sealed record DuplicateArticleDto(Guid Id, string Title, string Source);
