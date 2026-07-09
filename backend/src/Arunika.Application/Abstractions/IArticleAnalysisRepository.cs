using Arunika.Domain.Entities;

namespace Arunika.Application.Abstractions;

/// <summary>
/// Persistence contract for AI enrichment results (design doc §5/Phase 5).
/// Implemented in Infrastructure (EF Core) — resolves category/sector/keyword
/// lookups and writes <see cref="ArticleAnalysis"/> plus the related rows in
/// one call, and flips <see cref="Article.EnrichmentStatus"/> accordingly.
/// </summary>
public interface IArticleAnalysisRepository
{
    Task<Article?> GetArticleForEnrichmentAsync(Guid articleId, CancellationToken cancellationToken = default);

    Task SaveAnalysisAsync(Guid articleId, ArticleAnalysisResult result, CancellationToken cancellationToken = default);

    Task MarkEnrichmentFailedAsync(Guid articleId, CancellationToken cancellationToken = default);
}
