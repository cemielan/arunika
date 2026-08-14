using System.Text;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// <summary>
    /// Runs the digest and returns the number of emails actually sent (0 when
    /// there was nothing to send or no subscribers matched).
    /// </summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
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
            return 0;
        }

        var users = await userRepository.GetDigestSubscribersAsync(cancellationToken);

        if (users.Count == 0)
        {
            logger.LogInformation("SendEmailDigestJob: no digest subscribers; skipping.");
            return 0;
        }

        var digestHtml = BuildDigestHtml(briefing, articles, emailOptions.Value.FrontendUrl);

        foreach (var user in users)
        {
            try
            {
                await emailService.SendAsync(
                    user.Email,
                    $"Arunika Daily Digest — {now:MMMM dd, yyyy}",
                    digestHtml,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SendEmailDigestJob: failed to send digest to {Email}.", user.Email);
            }
        }

        logger.LogInformation("SendEmailDigestJob: sent digest to {Count} user(s) with {ArticleCount} articles.",
            users.Count, articles.Count);

        return users.Count;
    }

    private static string BuildDigestHtml(Briefing? briefing, IReadOnlyList<Article> articles, string frontendUrl)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><style>
              body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #1a1a1a; }
              h1 { font-size: 22px; margin-bottom: 4px; }
              .meta { color: #666; font-size: 13px; margin-bottom: 20px; }
              .summary { background: #f6f6f6; border-radius: 8px; padding: 16px; margin-bottom: 24px; }
              .summary h2 { font-size: 16px; margin: 0 0 8px; }
              .summary p { font-size: 13px; line-height: 1.6; color: #333; margin: 0; }
              .tag { display: inline-block; background: #e8e8e8; padding: 2px 8px; border-radius: 4px; font-size: 11px; margin-right: 4px; margin-bottom: 8px; }
              .story { padding: 12px 0; border-bottom: 1px solid #eee; }
              .story:last-child { border-bottom: none; }
              .story h3 { font-size: 15px; margin: 0 0 4px; }
              .story p { font-size: 13px; color: #444; margin: 0; }
              .footer { margin-top: 24px; font-size: 12px; color: #999; }
            </style></head>
            <body>
            <h1>Arunika Daily Digest</h1>
            <p class="meta">{Date} — {Count} top stories</p>
            """);

        var date = DateTimeOffset.UtcNow.ToString("MMMM dd, yyyy");
        sb.Replace("{Date}", date);
        sb.Replace("{Count}", articles.Count.ToString());
        sb.Replace("{FrontendUrl}", frontendUrl);

        if (briefing is not null)
        {
            sb.Append("<div class=\"summary\">");
            sb.Append("<h2>Today's Summary</h2>");
            sb.Append("<p>");
            if (!string.IsNullOrEmpty(briefing.OverallSentiment.ToString()))
                sb.Append($"<span class=\"tag\">{EscapeHtml(briefing.OverallSentiment.ToString())}</span>");
            if (!string.IsNullOrEmpty(briefing.RiskLevel.ToString()))
                sb.Append($"<span class=\"tag\">{EscapeHtml(briefing.RiskLevel.ToString())} risk</span>");
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
            sb.Append($"<h3>{EscapeHtml(article.Title)}</h3>");
            sb.Append($"<p>{EscapeHtml(summary)}</p>");
            sb.Append("<p>");
            if (!string.IsNullOrEmpty(category))
                sb.Append($"<span class=\"tag\">{EscapeHtml(category)}</span>");
            if (!string.IsNullOrEmpty(sentiment))
                sb.Append($"<span class=\"tag\">{EscapeHtml(sentiment)}</span>");
            sb.Append("</p></div>");
        }

        sb.Append("""
            <p class="footer">You are receiving this because you subscribed to the Arunika daily digest.
            <a href="{FrontendUrl}">Arunika</a></p>
            </body></html>
            """);

        return sb.ToString();
    }

    private static DateOnly JakartaToday()
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
    }

    private static string EscapeHtml(string text)
        => System.Net.WebUtility.HtmlEncode(text);
}
