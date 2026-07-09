namespace Arunika.Domain.Entities;

public class BriefingItem
{
    public Guid BriefingId { get; set; }
    public Guid ArticleId { get; set; }
    public int Rank { get; set; }

    public Briefing? Briefing { get; set; }
    public Article? Article { get; set; }
}
