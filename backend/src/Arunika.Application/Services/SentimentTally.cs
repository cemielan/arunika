using Arunika.Domain.Entities;
using Arunika.Domain.Enums;

namespace Arunika.Application.Services;

/// <summary>
/// Bullish / bearish / neutral split and average impact score over a set of
/// enriched articles. The briefing's market pulse, the sector board and the
/// stored snapshots of both are all counted here, so a live figure and its
/// archived copy can never disagree about how they were computed.
/// </summary>
public sealed record SentimentTally(int BullishCount, int BearishCount, int NeutralCount, int AverageImpactScore)
{
    public int Total => BullishCount + BearishCount + NeutralCount;

    /// <summary>Bullish or Bearish only on a strict plurality; anything else reads as Neutral.</summary>
    public Sentiment Dominant =>
        BullishCount > BearishCount && BullishCount > NeutralCount ? Sentiment.Bullish
        : BearishCount > BullishCount && BearishCount > NeutralCount ? Sentiment.Bearish
        : Sentiment.Neutral;

    /// <summary>Every article must have its <see cref="Article.Analysis"/> loaded.</summary>
    public static SentimentTally From(IReadOnlyCollection<Article> articles)
    {
        var total = articles.Count;
        var averageImpact = total > 0 ? (int)Math.Round(articles.Average(a => a.Analysis!.ImpactScore)) : 0;

        return new SentimentTally(
            articles.Count(a => a.Analysis!.Sentiment == Sentiment.Bullish),
            articles.Count(a => a.Analysis!.Sentiment == Sentiment.Bearish),
            articles.Count(a => a.Analysis!.Sentiment == Sentiment.Neutral),
            averageImpact);
    }

    /// <summary>
    /// One tally per sector, counting each article once for every sector it
    /// touches. Articles must have <see cref="Article.SectorImpacts"/> and their
    /// <see cref="ArticleSectorImpact.Sector"/> loaded.
    /// </summary>
    public static IReadOnlyList<SectorTally> BySector(IEnumerable<Article> articles)
        => articles
            .SelectMany(a => a.SectorImpacts, (article, impact) => (article, impact))
            .GroupBy(x => x.impact.SectorId)
            .Select(g => new SectorTally(
                g.Key,
                g.First().impact.Sector?.Name ?? "Unknown",
                From(g.Select(x => x.article).ToList())))
            .ToList();
}

public sealed record SectorTally(Guid SectorId, string Sector, SentimentTally Tally);
