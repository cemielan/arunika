using Arunika.Domain.Entities;

namespace Arunika.Application.Abstractions;

/// <summary>
/// Persistence contract for articles. Implemented in Infrastructure (EF Core).
/// </summary>
public interface IArticleRepository
{
    Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Article>> GetRecentForDedupeCheckAsync(DateTimeOffset since, CancellationToken cancellationToken = default);

    Task AddAsync(Article article, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
