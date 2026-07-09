using Arunika.Domain.Enums;

namespace Arunika.Domain.Entities;

public class Article
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string RawContent { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public required string DedupeHash { get; set; }
    public Guid? DuplicateOfId { get; set; }
    public EnrichmentStatus EnrichmentStatus { get; set; } = EnrichmentStatus.Pending;

    public NewsSource? Source { get; set; }
    public Article? DuplicateOf { get; set; }
    public ArticleAnalysis? Analysis { get; set; }
    public ICollection<ArticleSectorImpact> SectorImpacts { get; set; } = new List<ArticleSectorImpact>();
    public ICollection<ArticleKeyword> Keywords { get; set; } = new List<ArticleKeyword>();
    public ICollection<BriefingItem> BriefingItems { get; set; } = new List<BriefingItem>();
}
