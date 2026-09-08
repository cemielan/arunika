using Arunika.Application.Abstractions;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

/// <summary>
/// Sweep job (design doc §5) that re-enqueues <see cref="EnrichArticleJob"/> for
/// every article still marked <c>Failed</c>. <see cref="EnrichArticleJob"/> already
/// retries transient Gemini/network errors internally, but some failures (rate
/// limits, brief outages) only clear up after a few minutes — this sweep makes
/// sure those articles eventually get a summary/impact score instead of staying
/// "Failed" forever. Safe to run repeatedly: articles that succeed flip to
/// <c>Completed</c> and drop out of the next sweep automatically.
/// </summary>
[Queue("enrichment")]
public class RetryFailedEnrichmentJob(
    IArticleAnalysisRepository articleAnalysisRepository,
    ILogger<RetryFailedEnrichmentJob> logger)
{
    private const int MaxPerSweep = 50;
    private const int MaxRetriesPerArticle = 5;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var failedIds = await articleAnalysisRepository.GetFailedArticleIdsAsync(MaxPerSweep, cancellationToken);
        if (failedIds.Count == 0)
        {
            return;
        }

        logger.LogInformation("RetryFailedEnrichmentJob: re-enqueuing {Count} failed article(s) for enrichment.",
            failedIds.Count);

        foreach (var articleId in failedIds)
        {
            var retryCount = await articleAnalysisRepository.GetEnrichmentRetryCountAsync(articleId, cancellationToken);
            if (retryCount >= MaxRetriesPerArticle)
            {
                logger.LogWarning("RetryFailedEnrichmentJob: article {ArticleId} exceeded max retries ({MaxRetries}); skipping.", articleId, MaxRetriesPerArticle);
                continue;
            }

            var delay = TimeSpan.FromMinutes(Math.Pow(2, retryCount)); // 1m, 2m, 4m, 8m, 16m
            logger.LogInformation("RetryFailedEnrichmentJob: scheduling article {ArticleId} retry #{RetryCount} in {Delay}.", articleId, retryCount + 1, delay);

            BackgroundJob.Schedule<EnrichArticleJob>(
                job => job.RunAsync(articleId, CancellationToken.None),
                delay);
        }
    }
}
