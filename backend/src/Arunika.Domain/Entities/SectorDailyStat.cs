namespace Arunika.Domain.Entities;

/// <summary>
/// One sector's sentiment and impact for one Jakarta calendar day. Snapshotted
/// nightly before old articles are deleted, so the sector trend outlives the
/// article retention window.
/// </summary>
public class SectorDailyStat
{
    public DateOnly Date { get; set; }
    public Guid SectorId { get; set; }
    public int ArticleCount { get; set; }
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public int NeutralCount { get; set; }
    public int AverageImpactScore { get; set; }

    public Sector? Sector { get; set; }
}
