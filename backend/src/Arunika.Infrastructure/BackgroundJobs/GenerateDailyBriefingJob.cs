using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

public class GenerateDailyBriefingJob(
    IArticleRepository articleRepository,
    IArticleAnalysisRepository articleAnalysisRepository,
    IBriefingGenerationService briefingGenerationService,
    ILogger<GenerateDailyBriefingJob> logger)
{
    private const int TopStoryCount = 10;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var topStories = await articleRepository.GetTopByImpactScoreAsync(today, TopStoryCount, cancellationToken);

        if (topStories.Count == 0)
        {
            logger.LogInformation("GenerateDailyBriefingJob: no enriched articles for {Date}; skipping.", today);
            return;
        }

        logger.LogInformation("GenerateDailyBriefingJob: generating briefing for {Date} from {Count} top stories.",
            today, topStories.Count);

        var result = await briefingGenerationService.GenerateAsync(topStories, cancellationToken);

        var briefing = new Briefing
        {
            Id = Guid.NewGuid(),
            BriefingDate = today,
            ExecutiveSummary = result.ExecutiveSummary,
            OverallSentiment = result.OverallSentiment,
            RiskLevel = result.RiskLevel,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        for (var i = 0; i < topStories.Count; i++)
        {
            briefing.Items.Add(new BriefingItem
            {
                BriefingId = briefing.Id,
                ArticleId = topStories[i].Id,
                Rank = i + 1
            });
        }

        await articleAnalysisRepository.SaveBriefingAsync(briefing, cancellationToken);

        logger.LogInformation(
            "GenerateDailyBriefingJob: saved briefing for {Date} (sentiment {Sentiment}, risk {RiskLevel}).",
            today, result.OverallSentiment, result.RiskLevel);
    }
}
