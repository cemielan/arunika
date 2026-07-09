using Arunika.Domain.Enums;

namespace Arunika.Domain.Entities;

public class ArticleAnalysis
{
    public Guid ArticleId { get; set; }
    public required string Summary { get; set; }
    public Guid CategoryId { get; set; }
    public Sentiment Sentiment { get; set; }
    public float SentimentConfidence { get; set; }
    public int ImpactScore { get; set; }
    public string? ImpactRationale { get; set; }
    public required string ModelVersion { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    public Article? Article { get; set; }
    public Category? Category { get; set; }
}
