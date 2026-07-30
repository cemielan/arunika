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

    /// <summary>
    /// Persists a daily briefing (executive summary, sentiment, risk level, and
    /// linked top-story items). Replaces any existing briefing for the same date.
    /// </summary>
    Task SaveBriefingAsync(Briefing briefing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cached briefing for the given date, or null if none exists yet.
    /// Includes the linked <see cref="BriefingItem"/>s and their articles.
    /// </summary>
    Task<Briefing?> GetBriefingByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recently generated briefing regardless of date,
    /// or null if no briefing has ever been generated.
    /// </summary>
    Task<Briefing?> GetLatestBriefingAsync(CancellationToken cancellationToken = default);
}
