using System.Diagnostics;
using System.Text;
using Arunika.Application.Abstractions;
using Arunika.Application.Constants;
using Arunika.Domain.Entities;
using Arunika.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Arunika.Infrastructure.BackgroundJobs;

/// <summary>
/// Sends the daily email to digest subscribers at 08:30 WIB, 15 minutes after
/// <see cref="GenerateDailyBriefingJob"/> produces the day's briefing. The email
/// leads with the AI-generated executive summary shown on the dashboard and then
/// lists the top stories that back it (falling back to the last 24h of enriched
/// articles when no briefing exists yet).
/// </summary>
public class SendEmailDigestJob(
    IArticleRepository articleRepository,
    IArticleAnalysisRepository articleAnalysisRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<SendEmailDigestJob> logger)
{
    /// <summary>Per-run outcome; <see cref="Error"/> is the first failure seen.</summary>
    public sealed record DigestSendRunResult(int Sent, int Failed, string? Error);

    /// <summary>
    /// Runs the digest and returns the number of emails actually sent (0 when
    /// there was nothing to send or no subscribers matched).
    /// </summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
        => (await RunWithReportAsync(cancellationToken)).Sent;

    /// <summary>
    /// Runs the digest and reports how many emails were actually delivered vs
    /// failed, so manual/triggered runs can be diagnosed.
    /// </summary>
    public async Task<DigestSendRunResult> RunWithReportAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var briefing = await articleAnalysisRepository.GetBriefingByDateAsync(JakartaToday(), cancellationToken)
            ?? await articleAnalysisRepository.GetLatestBriefingAsync(cancellationToken);

        var articles = briefing is not null
            ? briefing.Items
                .OrderBy(item => item.Rank)
                .Select(item => item.Article)
                .Where(article => article is not null)
                .Cast<Article>()
                .ToList()
            : await articleRepository.GetEnrichedArticlesInRangeAsync(now.AddHours(-24), now, cancellationToken);

        if (articles.Count == 0)
        {
            logger.LogInformation("SendEmailDigestJob: no enriched articles or briefing available; skipping.");
            return new DigestSendRunResult(0, 0, "No articles or briefing available to include in the digest.");
        }

        var users = await userRepository.GetDigestSubscribersAsync(cancellationToken);

        if (users.Count == 0)
        {
            logger.LogInformation("SendEmailDigestJob: no digest subscribers; skipping.");
            return new DigestSendRunResult(0, 0, "No digest subscribers (users with EmailVerified = true).");
        }

        var digestHtml = BuildDigestHtml(briefing, articles, emailOptions.Value.FrontendUrl);

        var sent = 0;
        var failed = 0;
        string? firstError = null;

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, ctx) =>
                {
                    var email = (string)ctx["email"]!;
                    var corrId = (string)ctx["correlationId"]!;
                    logger.LogWarning(ex, "SendEmailDigestJob: retry {Attempt}/3 for {Email} after {Delay} (correlationId: {CorrelationId}).", attempt, email, delay, corrId);
                });

        foreach (var user in users)
        {
            var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N")[..12];
            try
            {
                await retryPolicy.ExecuteAsync(async (ctx, ct) =>
                {
                    await emailService.SendAsync(
                        user.Email,
                        $"Arunika Daily Digest — {now:MMMM dd, yyyy}",
                        digestHtml,
                        ct);
                }, new Context { ["email"] = user.Email, ["correlationId"] = correlationId }, cancellationToken);
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                firstError ??= $"{user.Email}: {ex.Message}";
                logger.LogError(ex, "SendEmailDigestJob: failed to send digest to {Email} (correlationId: {CorrelationId}).", user.Email, correlationId);
            }
        }

        logger.LogInformation("SendEmailDigestJob: sent digest to {Sent} user(s) with {ArticleCount} articles ({Failed} failed).",
            sent, articles.Count, failed);

        return new DigestSendRunResult(sent, failed, firstError);
    }

    private static string BuildDigestHtml(Briefing? briefing, IReadOnlyList<Article> articles, string frontendUrl)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><style>
              body { font-family: Georgia, "Times New Roman", serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #1a1a1a; background: #ffffff; }
              h1 { font-family: Georgia, "Times New Roman", serif; font-size: 22px; font-weight: 600; margin-bottom: 4px; border-top: 2px solid #1a1a1a; padding-top: 12px; }
              .meta { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; text-transform: uppercase; letter-spacing: 0.05em; color: #666; font-size: 11px; margin-bottom: 20px; }
              .summary { border: 1px solid #ddd; padding: 16px; margin-bottom: 24px; }
              .summary h2 { font-family: Georgia, "Times New Roman", serif; font-size: 16px; margin: 0 0 8px; }
              .summary p { font-size: 13px; line-height: 1.6; color: #333; margin: 0; }
              .tag { display: inline-block; border: 1px solid #999; color: #444; padding: 1px 7px; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; font-size: 11px; margin-right: 4px; margin-bottom: 8px; }
              .tag-bullish { border-color: #15803d; color: #15803d; }
              .tag-bearish { border-color: #b91c1c; color: #b91c1c; }
              .tag-high { border-color: #b91c1c; color: #b91c1c; }
              .tag-medium { border-color: #b45309; color: #b45309; }
              .tag-low { border-color: #15803d; color: #15803d; }
              .story { padding: 12px 0; border-bottom: 1px solid #ddd; }
              .story:last-child { border-bottom: none; }
              .story h3 { font-family: Georgia, "Times New Roman", serif; font-size: 15px; margin: 0 0 4px; }
              .story h3 a { color: #1a1a1a; text-decoration: none; }
              .story p { font-size: 13px; color: #444; margin: 0; }
              .cta { display: inline-block; background: #1a1a1a; color: #ffffff; text-decoration: none; padding: 10px 20px; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; font-size: 14px; font-weight: 600; margin: 8px 0 24px; }
              .footer { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; margin-top: 24px; font-size: 12px; color: #999; }
            </style></head>
            <body>
            <h1>Arunika Daily Digest</h1>
            <p class="meta">{Date} — {Count} top stories</p>
            <a class="cta" href="{FrontendUrl}" style="background:#1a1a1a !important; color:#fff !important; text-decoration:none;">Open Arunika →</a>
            """);

        if (briefing is not null)
        {
            sb.Append("<div class=\"summary\">");
            sb.Append("<h2>Today's Summary</h2>");
            sb.Append("<p>");
            if (!string.IsNullOrEmpty(briefing.OverallSentiment.ToString()))
                sb.Append($"<span class=\"tag {SentimentClass(briefing.OverallSentiment.ToString())}\">{EscapeHtml(briefing.OverallSentiment.ToString())}</span>");
            if (!string.IsNullOrEmpty(briefing.RiskLevel.ToString()))
                sb.Append($"<span class=\"tag {RiskClass(briefing.RiskLevel.ToString())}\">{EscapeHtml(briefing.RiskLevel.ToString())} risk</span>");
            sb.Append("</p>");
            sb.Append($"<p>{EscapeHtml(briefing.ExecutiveSummary)}</p>");
            sb.Append("</div>");
        }

        foreach (var article in articles.Take(10))
        {
            var summary = article.Analysis?.Summary ?? "No summary available.";
            var category = article.Analysis?.Category?.Name ?? "";
            var sentiment = article.Analysis?.Sentiment.ToString() ?? "";

            sb.Append("<div class=\"story\">");
            sb.Append($"<h3><a href=\"{EscapeHtml(frontendUrl)}\">{EscapeHtml(article.Title)}</a></h3>");
            sb.Append($"<p>{EscapeHtml(summary)}</p>");
            sb.Append("<p>");
            if (!string.IsNullOrEmpty(category))
                sb.Append($"<span class=\"tag\">{EscapeHtml(category)}</span>");
            if (!string.IsNullOrEmpty(sentiment))
                sb.Append($"<span class=\"tag {SentimentClass(sentiment)}\">{EscapeHtml(sentiment)}</span>");
            sb.Append("</p></div>");
        }

        sb.Append("""
            <p class="footer">You are receiving this because you subscribed to the Arunika daily digest.
            <a href="{FrontendUrl}">Arunika</a></p>
            </body></html>
            """);

        var date = DateTimeOffset.UtcNow.ToString("MMMM dd, yyyy");
        sb.Replace("{Date}", date);
        sb.Replace("{Count}", articles.Count.ToString());
        sb.Replace("{FrontendUrl}", frontendUrl);

        return sb.ToString();
    }

    private static string SentimentClass(string sentiment) => sentiment.ToLowerInvariant() switch
    {
        "bullish" => "tag-bullish",
        "bearish" => "tag-bearish",
        _ => ""
    };

    private static string RiskClass(string risk) => risk.ToLowerInvariant() switch
    {
        "high" => "tag-high",
        "medium" => "tag-medium",
        "low" => "tag-low",
        _ => ""
    };

    private static DateOnly JakartaToday()
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConstants.JakartaIana);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConstants.JakartaWindows);
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
    }

    private static string EscapeHtml(string text)
        => System.Net.WebUtility.HtmlEncode(text);
}
