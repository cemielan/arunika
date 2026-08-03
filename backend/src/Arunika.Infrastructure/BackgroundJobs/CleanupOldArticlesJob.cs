using Arunika.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

public class CleanupOldArticlesJob(
    IArticleRepository articleRepository,
    ILogger<CleanupOldArticlesJob> logger)
{
    private const int RetentionDays = 7;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);
        var deleted = await articleRepository.DeleteOlderThanAsync(cutoff, cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation(
                "CleanupOldArticlesJob: deleted {Count} article(s) published before {Cutoff}.",
                deleted, cutoff);
        }
    }
}
