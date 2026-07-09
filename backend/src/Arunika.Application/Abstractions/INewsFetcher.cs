namespace Arunika.Application.Abstractions;

/// <summary>
/// A single normalized article as pulled from a news source, before it is
/// persisted, deduplicated, or enriched.
/// </summary>
public record FetchedArticle(
    string Title,
    string Url,
    string RawContent,
    DateTimeOffset PublishedAt
);

/// <summary>
/// Pulls articles from one external news source (RSS feed, or a news/market
/// data API such as Financial Modeling Prep). Implemented in Infrastructure,
/// one implementation per source.
/// </summary>
public interface INewsFetcher
{
    /// <summary>Matches the corresponding <c>NewsSource.Name</c> row.</summary>
    string SourceName { get; }

    Task<IReadOnlyList<FetchedArticle>> FetchAsync(CancellationToken cancellationToken = default);
}
