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
                // Fetch more frequently so feeds keep moving even if the app
                // was asleep/restarted around a previous run window.
                recurringJobManager.AddOrUpdate<FetchNewsJob>(
                    "fetch-news",
                    job => job.RunAsync(CancellationToken.None),
                    "*/30 * * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });
                recurringJobManager.AddOrUpdate<GenerateDailyBriefingJob>(
                    "generate-daily-briefing",
                    job => job.RunAsync(CancellationToken.None),
                    "15 8 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });
                recurringJobManager.AddOrUpdate<CleanupOldArticlesJob>(
                    "cleanup-old-articles",
                    job => job.RunAsync(CancellationToken.None),
                    "20 0 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });
                // Pause the automatic retry sweep while Gemini quota is unstable.
                recurringJobManager.RemoveIfExists("retry-failed-enrichment");
                recurringJobManager.AddOrUpdate<SendEmailDigestJob>(
                    "send-email-digest",
                    job => job.RunAsync(CancellationToken.None),
                    "30 8 * * *",
                    new RecurringJobOptions { TimeZone = JakartaTimeZone() });

                // Catch up immediately on startup so a just-woken instance does
                // not wait up to 30 minutes before ingesting fresh feed items.
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
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}