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

    /// <summary>
    /// Ids of articles currently marked <see cref="Arunika.Domain.Enums.EnrichmentStatus.Failed"/>
    /// (e.g. transient Gemini/network errors) — used by <c>RetryFailedEnrichmentJob</c> to
    /// automatically re-run enrichment so a failure is never permanent (design doc §5).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetFailedArticleIdsAsync(int maxCount, CancellationToken cancellationToken = default);
}
