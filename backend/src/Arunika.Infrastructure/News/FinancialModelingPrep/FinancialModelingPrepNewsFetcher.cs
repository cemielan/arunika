using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.News.FinancialModelingPrep;

/// <summary>
/// Pulls broad market/economy/politics coverage from FMP's General News API
/// (as opposed to their per-symbol Stock News API).
/// </summary>
public class FinancialModelingPrepNewsFetcher(
    HttpClient httpClient,
    IOptions<FinancialModelingPrepOptions> options,
    ILogger<FinancialModelingPrepNewsFetcher> logger) : INewsFetcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string SourceName => "Financial Modeling Prep";

    public async Task<IReadOnlyList<FetchedArticle>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Skipping {Source} fetch: no API key configured.", SourceName);
            return [];
        }

        var requestUri = $"stable/news/general-latest?page=0&limit=100&apikey={apiKey}";
        var items = await httpClient.GetFromJsonAsync<List<FmpNewsArticleDto>>(requestUri, JsonOptions, cancellationToken)
            ?? [];

        var results = new List<FetchedArticle>(items.Count);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Url) || string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            // FMP returns "yyyy-MM-dd HH:mm:ss" with no timezone offset in the payload;
            // treat it as UTC until proven otherwise against real data.
            if (!DateTime.TryParseExact(item.PublishedDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var publishedAtUtc))
            {
                logger.LogWarning("Skipping article with unparseable publishedDate {PublishedDate} from {Source}.",
                    item.PublishedDate, SourceName);
                continue;
            }

            results.Add(new FetchedArticle(
                Title: item.Title,
                Url: item.Url,
                RawContent: item.Text ?? string.Empty,
                PublishedAt: new DateTimeOffset(publishedAtUtc, TimeSpan.Zero)
            ));
        }

        return results;
    }
}
