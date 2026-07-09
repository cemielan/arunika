using Arunika.Domain.Enums;

namespace Arunika.Domain.Entities;

public class ArticleSectorImpact
{
    public Guid ArticleId { get; set; }
    public Guid SectorId { get; set; }
    public ImpactDirection Direction { get; set; }
    public int Magnitude { get; set; }

    public Article? Article { get; set; }
    public Sector? Sector { get; set; }
}
