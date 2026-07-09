namespace Arunika.Api.Contracts;

/// <summary>
/// One row of <c>GET /v1/news</c>. AI-derived fields are null until the
/// article's enrichment (Phase 5) has completed.
/// </summary>
public sealed record NewsListItemDto(
    Guid Id,
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string? Category,
    string? Sentiment,
    int? ImpactScore,
    string? Summary);
