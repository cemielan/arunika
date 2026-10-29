using Arunika.Domain.Entities;

namespace Arunika.Application.Services;

/// <summary>
/// Picks the briefing's top stories. Title-similarity dedupe (DuplicateOfId)
/// only catches near-identical headlines; one event reported under different
/// headlines ("Brazilian markets crash after Lula's win", "Markets don't vote
/// for Lula") still slips through and can fill several of the ten slots. A
/// story is therefore skipped when its keywords overlap heavily with a story
/// already picked — the higher-impact one stands for the event.
/// </summary>
public static class TopStorySelector
{
    // ponytail: keyword-overlap heuristic, swap for embedding similarity if it mis-groups.
    private const int MinSharedKeywords = 3;
    private const double MinOverlap = 0.4;

    /// <param name="articles">Enriched articles, already in ranking order.</param>
    public static List<Article> Select(IEnumerable<Article> articles, int count)
    {
        var picked = new List<(Article Article, HashSet<string> Keywords)>();

        foreach (var article in articles)
        {
            var keywords = KeywordSet(article);
            if (picked.Any(p => IsSameEvent(keywords, p.Keywords)))
            {
                continue;
            }

            picked.Add((article, keywords));
            if (picked.Count == count)
            {
                break;
            }
        }

        return picked.Select(p => p.Article).ToList();
    }

    private static HashSet<string> KeywordSet(Article article) =>
        article.Keywords
            .Select(k => k.Keyword?.Text.Trim().ToLowerInvariant())
            .Where(text => !string.IsNullOrEmpty(text))
            .ToHashSet()!;

    // Overlap is measured against the smaller set, so a story with few keywords
    // can still be recognised as part of a larger one.
    private static bool IsSameEvent(HashSet<string> a, HashSet<string> b)
    {
        var shared = a.Count(b.Contains);
        return shared >= MinSharedKeywords && shared >= MinOverlap * Math.Min(a.Count, b.Count);
    }
}
