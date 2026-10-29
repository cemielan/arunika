namespace Arunika.Infrastructure.BackgroundJobs;

public class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>
    /// How long an article is kept after its publish date. Articles ranked in a
    /// briefing are kept regardless. Never below 7: the briefing, market pulse
    /// and sector board all read a rolling 7-day window of articles.
    /// </summary>
    public int ArticleDays { get; set; } = 7;
}
