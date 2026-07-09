namespace Arunika.Application.Abstractions;

/// <summary>
/// Read access to the (mostly static/seeded) news_sources table — resolves an
/// <see cref="INewsFetcher.SourceName"/> to the NewsSource row it corresponds to.
/// </summary>
public interface INewsSourceRepository
{
    Task<Guid?> GetIdByNameAsync(string name, CancellationToken cancellationToken = default);
}
