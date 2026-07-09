using Arunika.Domain.Entities;

namespace Arunika.Application.Abstractions;

/// <summary>
/// Persistence contract for articles. Implemented in Infrastructure (EF Core).
/// </summary>
public interface IArticleRepository
{
    Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Design doc §6 dedup order: exact match (dedupe hash) first, then a
    /// title-similarity check (Postgres trigram similarity) against articles
    /// published since <paramref name="since"/>. Duplicates-of-duplicates are
    /// excluded so every match points at the earliest canonical article.
    /// Returns null when the candidate is unique.
    /// </summary>
    Task<Article?> FindDuplicateAsync(string dedupeHash, string title, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task AddAsync(Article article, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6 (design doc §7) — <c>GET /v1/news</c>. Non-duplicate articles,
    /// newest first, optionally filtered by category name. Includes the
    /// source and analysis/category needed to render the list without extra
    /// round-trips.
    /// </summary>
    Task<(IReadOnlyList<Article> Items, int TotalItems)> GetFeedAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6 — <c>GET /v1/articles/{id}</c>. Loads everything needed for
    /// the full detail view: source, analysis/category, sector impacts, and
    /// keywords. Returns null when no article with that id exists.
    /// </summary>
    Task<Article?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Other articles that were linked as duplicates of <paramref name="canonicalArticleId"/>
    /// — used to surface "also reported by" coverage on the article detail view.
    /// </summary>
    Task<IReadOnlyList<Article>> GetDuplicatesOfAsync(Guid canonicalArticleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6 — <c>GET /v1/briefing</c> (simple version per the to-do list:
    /// top N non-duplicate, enriched articles by impact score for the given
    /// date — no generated executive summary yet).
    /// </summary>
    Task<IReadOnlyList<Article>> GetTopByImpactScoreAsync(DateOnly date, int take, CancellationToken cancellationToken = default);
}
