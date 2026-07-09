using Arunika.Application.Abstractions;
using Arunika.Application.Services;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

/// <summary>
/// Pulls from every registered <see cref="INewsFetcher"/>, skips articles that
/// already exist (by URL), and persists the rest. One fetcher failing does not
/// stop the others (design doc §3/Phase 3: "a failed source shouldn't take down
/// the whole job").
/// </summary>
public class FetchNewsJob(
    IEnumerable<INewsFetcher> fetchers,
    IArticleRepository articleRepository,
    INewsSourceRepository newsSourceRepository,
    ILogger<FetchNewsJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var fetcher in fetchers)
        {
            try
            {
                await RunForSourceAsync(fetcher, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FetchNewsJob failed for source {Source}; continuing with remaining sources.",
                    fetcher.SourceName);
            }
        }
    }

    private async Task RunForSourceAsync(INewsFetcher fetcher, CancellationToken cancellationToken)
    {
        var sourceId = await newsSourceRepository.GetIdByNameAsync(fetcher.SourceName, cancellationToken);
        if (sourceId is null)
        {
            logger.LogError(
                "No news_sources row named '{Source}' — skipping. Seed it via an EF Core configuration/migration first.",
                fetcher.SourceName);
            return;
        }

        var fetched = await fetcher.FetchAsync(cancellationToken);
        var savedCount = 0;

        foreach (var item in fetched)
        {
            var existing = await articleRepository.GetByUrlAsync(item.Url, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            var article = new Article
            {
                Id = Guid.NewGuid(),
                SourceId = sourceId.Value,
                Title = item.Title,
                Url = item.Url,
                RawContent = item.RawContent,
                PublishedAt = item.PublishedAt,
                FetchedAt = now,
                DedupeHash = DedupeHasher.ComputeHash(item.Title, fetcher.SourceName)
            };

            await articleRepository.AddAsync(article, cancellationToken);
            savedCount++;
        }

        await articleRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Source}: fetched {FetchedCount}, saved {SavedCount} new article(s).",
            fetcher.SourceName, fetched.Count, savedCount);
    }
}
