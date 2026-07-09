using System.ServiceModel.Syndication;
using System.Xml;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.News.Rss;

/// <summary>
/// Generic RSS/Atom fetcher — one instance per configured feed. Free, no API
/// key required, unlike FMP's paywalled news endpoints.
/// </summary>
public class CnbcRssNewsFetcher(HttpClient httpClient, ILogger<CnbcRssNewsFetcher> logger) : INewsFetcher
{
    private const string FeedUrl = "https://www.cnbc.com/id/100003114/device/rss/rss.html";

    public string SourceName => "CNBC";

    public async Task<IReadOnlyList<FetchedArticle>> FetchAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await httpClient.GetStreamAsync(FeedUrl, cancellationToken);
        using var reader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(reader);

        var results = new List<FetchedArticle>();
        foreach (var item in feed.Items)
        {
            var url = item.Links.FirstOrDefault()?.Uri?.ToString();
            var title = item.Title?.Text;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var publishedAt = item.PublishDate != default ? item.PublishDate : DateTimeOffset.UtcNow;
            var rawContent = item.Summary?.Text ?? string.Empty;

            results.Add(new FetchedArticle(title, url, rawContent, publishedAt));
        }

        logger.LogInformation("{Source}: parsed {Count} item(s) from feed.", SourceName, results.Count);
        return results;
    }
}
