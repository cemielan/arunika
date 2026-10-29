using Arunika.Domain.Enums;

namespace Arunika.Domain.Entities;

public class Briefing
{
    public Guid Id { get; set; }
    public DateOnly BriefingDate { get; set; }
    public required string ExecutiveSummary { get; set; }
    public string? ExecutiveSummaryIndonesian { get; set; }
    public Sentiment OverallSentiment { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    // Market pulse of the briefing's window as it stood when it was generated.
    // The window's articles are deleted after the retention period, so an
    // archived briefing can't recount them. All zero on briefings saved before
    // these columns existed.
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public int NeutralCount { get; set; }
    public int AverageImpactScore { get; set; }

    public ICollection<BriefingItem> Items { get; set; } = new List<BriefingItem>();
}
