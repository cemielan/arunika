using System.Text;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.BackgroundJobs;

public class SendEmailDigestJob(
    IArticleRepository articleRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    ILogger<SendEmailDigestJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddHours(-24);

        var articles = await articleRepository.GetEnrichedArticlesInRangeAsync(from, now, cancellationToken);

        if (articles.Count == 0)
        {
            logger.LogInformation("SendEmailDigestJob: no enriched articles in the last 24h; skipping.");
            return;
        }

        var digestHtml = BuildDigestHtml(articles);

        var users = await userRepository.GetDigestSubscribersAsync(cancellationToken);

        if (users.Count == 0)
        {
            logger.LogInformation("SendEmailDigestJob: no digest subscribers; skipping.");
            return;
        }

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
    }

    private static string BuildDigestHtml(IReadOnlyList<Article> articles)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><style>
              body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #1a1a1a; }
              h1 { font-size: 22px; margin-bottom: 4px; }
              .meta { color: #666; font-size: 13px; margin-bottom: 20px; }
              .story { padding: 12px 0; border-bottom: 1px solid #eee; }
              .story:last-child { border-bottom: none; }
              .story h2 { font-size: 16px; margin: 0 0 4px; }
              .story p { font-size: 13px; color: #444; margin: 0; }
              .tag { display: inline-block; background: #f0f0f0; padding: 2px 8px; border-radius: 4px; font-size: 11px; margin-right: 4px; }
              .footer { margin-top: 24px; font-size: 12px; color: #999; }
            </style></head>
            <body>
            <h1>Arunika Daily Digest</h1>
            <p class="meta">{Date} — {Count} top stories</p>
            """);

        var date = DateTimeOffset.UtcNow.ToString("MMMM dd, yyyy");
        sb.Replace("{Date}", date);
        sb.Replace("{Count}", articles.Count.ToString());

        foreach (var article in articles.Take(10))
        {
            var summary = article.Analysis?.Summary ?? "No summary available.";
            var category = article.Analysis?.Category?.Name ?? "";
            var sentiment = article.Analysis?.Sentiment.ToString() ?? "";

            sb.Append("<div class=\"story\">");
            sb.Append($"<h2>{EscapeHtml(article.Title)}</h2>");
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
            <a href="{UnsubscribeUrl}">Unsubscribe</a></p>
            </body></html>
            """);

        return sb.ToString();
    }

    private static string EscapeHtml(string text)
        => System.Net.WebUtility.HtmlEncode(text);
}
