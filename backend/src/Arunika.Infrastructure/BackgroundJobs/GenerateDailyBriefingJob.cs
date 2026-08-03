using Arunika.Application.Services;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

public class GenerateDailyBriefingJob(
    DailyBriefingService dailyBriefingService,
    ILogger<GenerateDailyBriefingJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, JakartaTimeZone()).DateTime);

        var briefing = await dailyBriefingService.GenerateForDateAsync(today, cancellationToken);

        if (briefing is null)
        {
            logger.LogInformation("GenerateDailyBriefingJob: no enriched articles for {Date}; skipping.", today);
            return;
        }

        logger.LogInformation(
            "GenerateDailyBriefingJob: saved briefing for {Date} (sentiment {Sentiment}, risk {RiskLevel}).",
            today, briefing.OverallSentiment, briefing.RiskLevel);
    }

    private static TimeZoneInfo JakartaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
