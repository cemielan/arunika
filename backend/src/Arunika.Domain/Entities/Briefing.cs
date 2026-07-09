using Arunika.Domain.Enums;

namespace Arunika.Domain.Entities;

public class Briefing
{
    public Guid Id { get; set; }
    public DateOnly BriefingDate { get; set; }
    public required string ExecutiveSummary { get; set; }
    public Sentiment OverallSentiment { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    public ICollection<BriefingItem> Items { get; set; } = new List<BriefingItem>();
}
