using Arunika.Application.Abstractions;
using Arunika.Domain.Enums;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace Arunika.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs the AI enrichment pipeline (design doc §5) for one unique article.
/// Enqueued fire-and-forget by <see cref="FetchNewsJob"/> right after a
/// non-duplicate article is saved.
/// </summary>
[Queue("enrichment")]
public class EnrichArticleJob(
    IAiEnrichmentService aiEnrichmentService,
    IArticleAnalysisRepository articleAnalysisRepository,
    ILogger<EnrichArticleJob> logger)
{
    public async Task RunAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var article = await articleAnalysisRepository.GetArticleForEnrichmentAsync(articleId, cancellationToken);
        if (article is null)
        {
            logger.LogWarning("EnrichArticleJob: article {ArticleId} not found; skipping.", articleId);
            return;
        }

        // An article that already has an analysis keeps it. SaveAnalysisAsync is an
        // idempotent upsert, so a job replayed after a worker died mid-run would
        // otherwise rescore an article that finished successfully — and since the
        // scoring rubric changes over time, that silently swaps one scale for
        // another on a row nobody asked to revisit. Re-enrichment is a deliberate
        // sweep, not something a duplicate job should trigger.
        if (article.EnrichmentStatus == EnrichmentStatus.Completed)
        {
            logger.LogDebug("EnrichArticleJob: article {ArticleId} is already enriched; skipping.", articleId);
            return;
        }

        try
        {
            var result = await aiEnrichmentService.AnalyzeAsync(article, cancellationToken);
            await articleAnalysisRepository.SaveAnalysisAsync(articleId, result, cancellationToken);
            logger.LogInformation("EnrichArticleJob: enriched article {ArticleId} (impact {ImpactScore}).",
                articleId, result.ImpactScore);
        }
        catch (Exception ex)
        {
            // The AI service already retried transient failures internally; a failure here
            // is final for this attempt. Mark it rather than blocking the pipeline (design
            // doc §5) — a future sweep job (RetryFailedEnrichmentJob) can retry later.
            logger.LogError(ex, "EnrichArticleJob: enrichment failed for article {ArticleId}; marking Failed.", articleId);
            await articleAnalysisRepository.MarkEnrichmentFailedAsync(articleId, cancellationToken);
        }
    }
}
