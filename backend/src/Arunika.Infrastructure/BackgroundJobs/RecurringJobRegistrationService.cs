using Arunika.Application.Constants;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

public sealed class RecurringJobRegistrationService(
    IRecurringJobManager recurringJobManager,
    IBackgroundJobClient backgroundJobClient,
    ILogger<RecurringJobRegistrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Fetch every 30 minutes so feeds keep moving
                recurringJobManager.AddOrUpdate<FetchNewsJob>(
                    "fetch-news",
                    job => job.RunAsync(CancellationToken.None),
                    "*/30 * * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

                // Briefing every 6 hours (0, 6, 12, 18)
                recurringJobManager.AddOrUpdate<GenerateDailyBriefingJob>(
                    "generate-briefing-6h",
                    job => job.RunAsync(CancellationToken.None),
                    "0 */6 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

                // Cleanup old articles at 12:20 AM
                recurringJobManager.AddOrUpdate<CleanupOldArticlesJob>(
                    "cleanup-old-articles",
                    job => job.RunAsync(CancellationToken.None),
                    "20 0 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

                // Retry failed enrichment every hour (rate limiter protects quota)
                recurringJobManager.AddOrUpdate<RetryFailedEnrichmentJob>(
                    "retry-failed-enrichment",
                    job => job.RunAsync(CancellationToken.None),
                    "0 * * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

                // Send email digest at 9:00 AM Jakarta time
                recurringJobManager.AddOrUpdate<SendEmailDigestJob>(
                    "send-email-digest",
                    job => job.RunAsync(CancellationToken.None),
                    "0 9 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

                // Catch up immediately on startup
                backgroundJobClient.Enqueue<FetchNewsJob>(job => job.RunAsync(CancellationToken.None));

                logger.LogInformation("Hangfire recurring jobs registered successfully.");
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex,
                    "Failed to register Hangfire recurring jobs. Retrying in 1 minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private static TimeZoneInfo JakartaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConstants.JakartaIana);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConstants.JakartaWindows);
        }
    }
}