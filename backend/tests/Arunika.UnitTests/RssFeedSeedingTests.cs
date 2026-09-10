using Arunika.Domain.Entities;
using Arunika.Infrastructure;
using Arunika.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Arunika.UnitTests;

/// <summary>
/// FetchNewsJob resolves a fetcher's source row by name and skips the feed when
/// no row matches, logging an error rather than failing. A feed registered in
/// DependencyInjection without a matching seeded news_sources row therefore
/// never ingests anything, and nothing in the build catches it — hence this test.
/// </summary>
public class RssFeedSeedingTests
{
    private static IReadOnlyList<string> SeededSourceNames()
    {
        var modelBuilder = new ModelBuilder();
        new NewsSourceConfiguration().Configure(modelBuilder.Entity<NewsSource>());

        return modelBuilder.Model
            .FindEntityType(typeof(NewsSource))!
            .GetSeedData()
            .Select(row => (string)row[nameof(NewsSource.Name)]!)
            .ToList();
    }

    [Fact]
    public void EveryRegisteredRssFeed_HasAMatchingSeededSourceName()
    {
        var seeded = SeededSourceNames();

        var unseeded = DependencyInjection.RssFeeds
            .Select(feed => feed.SourceName)
            .Where(name => !seeded.Contains(name))
            .ToList();

        Assert.Empty(unseeded);
    }

    [Fact]
    public void RegisteredRssFeeds_HaveNoDuplicateNamesOrUrls()
    {
        var feeds = DependencyInjection.RssFeeds;

        Assert.Equal(feeds.Length, feeds.Select(f => f.SourceName).Distinct().Count());
        Assert.Equal(feeds.Length, feeds.Select(f => f.FeedUrl).Distinct().Count());
    }
}
