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
}
