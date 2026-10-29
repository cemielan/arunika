using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Arunika.Infrastructure.BackgroundJobs;
using Arunika.Infrastructure.Persistence;
using Arunika.Infrastructure.Persistence.Configurations;
using Arunika.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arunika.IntegrationTests;

/// <summary>
/// Retention against a real Postgres: what the nightly cleanup keeps for the
/// briefing archive, the sector snapshot it takes before deleting, and the
/// per-story source count read off the duplicate links.
/// </summary>
/// <remarks>
/// The repository tests use dates in early 2025 and an explicit cutoff so they
/// only ever touch their own rows in the shared database.
/// </remarks>
[Collection(PostgresCollection.Name)]
public class ArticleRetentionTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset OldDate = new(2025, 2, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Cutoff = new(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeleteOlderThanAsync_KeepsBriefedArticlesAndTheirDuplicates()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Retention-Keep");

        var briefed = NewEnrichedArticle(sourceId, OldDate);
        var briefedDuplicate = NewArticle(sourceId, OldDate, duplicateOf: briefed.Id);
        var unbriefed = NewEnrichedArticle(sourceId, OldDate);
        // Newer than the cutoff, but it has to go with the story it duplicates.
        var unbriefedDuplicate = NewArticle(sourceId, Cutoff.AddDays(10), duplicateOf: unbriefed.Id);
        var fresh = NewEnrichedArticle(sourceId, Cutoff.AddDays(1));
        db.Articles.AddRange(briefed, briefedDuplicate, unbriefed, unbriefedDuplicate, fresh);

        var briefing = new Briefing
        {
            Id = Guid.NewGuid(),
            BriefingDate = DateOnly.FromDateTime(OldDate.UtcDateTime),
            ExecutiveSummary = "Archived summary.",
            GeneratedAt = OldDate,
        };
        briefing.Items.Add(new BriefingItem { BriefingId = briefing.Id, ArticleId = briefed.Id, Rank = 1 });
        db.Briefings.Add(briefing);
        await db.SaveChangesAsync();

        var deleted = await new ArticleRepository(db).DeleteOlderThanAsync(Cutoff);

        Assert.Equal(1, deleted);
        var remaining = await db.Articles.AsNoTracking()
            .Where(a => a.SourceId == sourceId)
            .Select(a => a.Id)
            .ToListAsync();
        Assert.Equivalent(new[] { briefed.Id, briefedDuplicate.Id, fresh.Id }, remaining);
        Assert.True(await db.ArticleAnalyses.AnyAsync(a => a.ArticleId == briefed.Id));
        Assert.False(await db.ArticleAnalyses.AnyAsync(a => a.ArticleId == unbriefed.Id));
        Assert.Equal(1, await db.BriefingItems.CountAsync(i => i.BriefingId == briefing.Id));

        // The archive reads the briefing back with everything a story card needs.
        db.ChangeTracker.Clear();
        var archived = await new ArticleAnalysisRepository(db).GetBriefingByDateAsync(briefing.BriefingDate);
        var story = Assert.Single(archived!.Items).Article!;
        Assert.Equal(briefed.Id, story.Id);
        Assert.NotNull(story.Source);
        Assert.NotNull(story.Analysis?.Category);
    }

    [Fact]
    public async Task GetSourceCountsAsync_CountsDistinctSourcesAcrossDuplicates()
    {
        await using var db = fixture.CreateDbContext();
        var first = await SeedSourceAsync(db, "Coverage-First");
        var second = await SeedSourceAsync(db, "Coverage-Second");
        var third = await SeedSourceAsync(db, "Coverage-Third");

        var story = NewEnrichedArticle(first, OldDate);
        var solo = NewEnrichedArticle(first, OldDate);
        db.Articles.AddRange(
            story,
            solo,
            NewArticle(second, OldDate, duplicateOf: story.Id),
            NewArticle(third, OldDate, duplicateOf: story.Id),
            // A second copy from a source already counted adds nothing.
            NewArticle(second, OldDate, duplicateOf: story.Id));
        await db.SaveChangesAsync();

        var counts = await new ArticleRepository(db).GetSourceCountsAsync([story.Id, solo.Id]);

        Assert.Equal(3, counts[story.Id]);
        Assert.Equal(1, counts[solo.Id]);
    }

    [Fact]
    public async Task CleanupJob_SnapshotsSectorStatsBeforeDeleting()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Retention-Snapshot");

        var jakarta = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        var yesterday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, jakarta).DateTime).AddDays(-1);
        // Noon in Jakarta, so the day can't shift whichever zone the test runs in.
        var noonYesterday = new DateTimeOffset(yesterday.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(7)).ToUniversalTime();

        var bullish = NewEnrichedArticle(sourceId, noonYesterday, Sentiment.Bullish, impactScore: 80);
        var bearish = NewEnrichedArticle(sourceId, noonYesterday, Sentiment.Bearish, impactScore: 40);
        foreach (var article in new[] { bullish, bearish })
        {
            article.SectorImpacts.Add(new ArticleSectorImpact
            {
                ArticleId = article.Id,
                SectorId = SectorConfiguration.Energy,
                Direction = ImpactDirection.Positive,
                Magnitude = 50,
            });
        }
        db.Articles.AddRange(bullish, bearish);
        await db.SaveChangesAsync();

        var job = new CleanupOldArticlesJob(
            new ArticleRepository(db),
            new SectorStatsRepository(db),
            Options.Create(new RetentionOptions()),
            NullLogger<CleanupOldArticlesJob>.Instance);
        await job.RunAsync();

        var stat = await db.SectorDailyStats.AsNoTracking()
            .SingleAsync(s => s.Date == yesterday && s.SectorId == SectorConfiguration.Energy);
        Assert.Equal(2, stat.ArticleCount);
        Assert.Equal(1, stat.BullishCount);
        Assert.Equal(1, stat.BearishCount);
        Assert.Equal(60, stat.AverageImpactScore);
    }

    private static Article NewArticle(Guid sourceId, DateTimeOffset publishedAt, Guid? duplicateOf = null) => new()
    {
        Id = Guid.NewGuid(),
        SourceId = sourceId,
        Title = "Retention test article",
        Url = $"https://example.com/{Guid.NewGuid():N}",
        RawContent = "Content body for test article.",
        PublishedAt = publishedAt,
        FetchedAt = publishedAt,
        DedupeHash = Guid.NewGuid().ToString("N"),
        DuplicateOfId = duplicateOf,
    };

    private static Article NewEnrichedArticle(
        Guid sourceId, DateTimeOffset publishedAt, Sentiment sentiment = Sentiment.Neutral, int impactScore = 50)
    {
        var article = NewArticle(sourceId, publishedAt);
        article.EnrichmentStatus = EnrichmentStatus.Completed;
        article.Analysis = new ArticleAnalysis
        {
            ArticleId = article.Id,
            Summary = "Test summary.",
            CategoryId = CategoryConfiguration.Markets,
            Sentiment = sentiment,
            SentimentConfidence = 0.5f,
            ImpactScore = impactScore,
            ModelVersion = "test-model",
            GeneratedAt = publishedAt,
        };
        return article;
    }

    private static async Task<Guid> SeedSourceAsync(ArunikaDbContext db, string name)
    {
        var source = new NewsSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            BaseUrl = "https://example.com",
            SourceType = SourceType.Rss,
            TrustScore = 50,
        };
        db.NewsSources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }
}
