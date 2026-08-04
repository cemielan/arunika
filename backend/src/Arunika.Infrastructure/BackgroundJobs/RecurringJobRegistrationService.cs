using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

public sealed class RecurringJobRegistrationService(
    IRecurringJobManager recurringJobManager,
    ILogger<RecurringJobRegistrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                recurringJobManager.AddOrUpdate<FetchNewsJob>(
                    "fetch-news",
                    job => job.RunAsync(CancellationToken.None),
                    "0 8,15 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });
                recurringJobManager.AddOrUpdate<GenerateDailyBriefingJob>(
                    "generate-daily-briefing",
                    job => job.RunAsync(CancellationToken.None),
                    "10 8 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });
                recurringJobManager.AddOrUpdate<CleanupOldArticlesJob>(
                    "cleanup-old-articles",
                    job => job.RunAsync(CancellationToken.None),
                    "20 0 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });
                recurringJobManager.AddOrUpdate<RetryFailedEnrichmentJob>(
                    "retry-failed-enrichment",
                    job => job.RunAsync(CancellationToken.None),
                    "*/10 * * * *");
                recurringJobManager.AddOrUpdate<SendEmailDigestJob>(
                    "send-email-digest",
                    job => job.RunAsync(CancellationToken.None),
                    "30 8 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

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
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}