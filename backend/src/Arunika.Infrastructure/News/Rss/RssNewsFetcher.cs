using System.ServiceModel.Syndication;
using System.Xml;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.News.Rss;

public class RssNewsFetcher : INewsFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RssNewsFetcher> _logger;
    private readonly string _sourceName;
    private readonly string _feedUrl;

    public RssNewsFetcher(string sourceName, string feedUrl, HttpClient httpClient, ILogger<RssNewsFetcher> logger)
    {
        _sourceName = sourceName;
        _feedUrl = feedUrl;
        _httpClient = httpClient;
        _logger = logger;
    }

    public string SourceName => _sourceName;

    public async Task<IReadOnlyList<FetchedArticle>> FetchAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await _httpClient.GetStreamAsync(_feedUrl, cancellationToken);
        using var reader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(reader);

        var results = new List<FetchedArticle>();
        foreach (var item in feed.Items)
        {
            var url = item.Links.FirstOrDefault()?.Uri?.ToString();
            var title = item.Title?.Text;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
                continue;

            var publishedAt = item.PublishDate != default ? item.PublishDate : DateTimeOffset.UtcNow;
            var rawContent = item.Summary?.Text ?? string.Empty;

            results.Add(new FetchedArticle(title, url, rawContent, publishedAt));
        }

        _logger.LogInformation("{Source}: parsed {Count} item(s) from feed.", _sourceName, results.Count);
        return results;
    }
}
