using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Arunika.Infrastructure.Persistence;
using Arunika.Infrastructure.Persistence.Configurations;
using Arunika.Infrastructure.Persistence.Repositories;

namespace Arunika.IntegrationTests;

/// <summary>
/// Exercises <see cref="ArticleRepository.GetTopByImpactScoreAsync"/> — the
/// briefing's "isn't just trust the model" logic (design doc §7 <c>GET
/// /v1/briefing</c>): ranking by <c>ImpactScore</c>, scoping to a single
/// calendar day, and excluding duplicates/un-enriched articles rather than
/// just trusting whatever the AI happened to return for every row in the table.
/// </summary>
/// <remarks>
/// Each test uses its own, non-adjacent <see cref="DateOnly"/> because all
/// tests in this class share one Postgres database via <see cref="PostgresCollection"/>
/// — <c>GetTopByImpactScoreAsync</c> only filters by date, so reusing a date
/// across tests would let one test's seeded rows leak into another's results.
/// </remarks>
[Collection(PostgresCollection.Name)]
public class ArticleRepositoryImpactRankingTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetTopByImpactScoreAsync_OrdersByImpactScoreDescending()
    {
        var date = new DateOnly(2026, 1, 10);
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Ranking-Order");

        var low = await SeedEnrichedArticleAsync(db, sourceId, "Low impact story", 20, date);
        var high = await SeedEnrichedArticleAsync(db, sourceId, "High impact story", 90, date);
        var medium = await SeedEnrichedArticleAsync(db, sourceId, "Medium impact story", 55, date);

        var repository = new ArticleRepository(db);
        var results = await repository.GetTopByImpactScoreAsync(date, take: 10);

        Assert.Equal([high.Id, medium.Id, low.Id], results.Select(a => a.Id).ToArray());
    }

    [Fact]
    public async Task GetTopByImpactScoreAsync_RespectsTakeLimit()
    {
        var date = new DateOnly(2026, 1, 15);
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Ranking-Take");

        var top = await SeedEnrichedArticleAsync(db, sourceId, "Top story for take-limit test", 95, date);
        await SeedEnrichedArticleAsync(db, sourceId, "Second story for take-limit test", 80, date);
        await SeedEnrichedArticleAsync(db, sourceId, "Third story for take-limit test", 70, date);

        var repository = new ArticleRepository(db);
        var results = await repository.GetTopByImpactScoreAsync(date, take: 1);

        Assert.Single(results);
        Assert.Equal(top.Id, results[0].Id);
    }

    [Fact]
    public async Task GetTopByImpactScoreAsync_ExcludesArticlesPublishedOnOtherDays()
    {
        var date = new DateOnly(2026, 1, 20);
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Ranking-OtherDay");

        var today = await SeedEnrichedArticleAsync(db, sourceId, "Story published today", 60, date);
        await SeedEnrichedArticleAsync(db, sourceId, "Story published yesterday", 99, date.AddDays(-1));
        await SeedEnrichedArticleAsync(db, sourceId, "Story published tomorrow", 99, date.AddDays(1));

        var repository = new ArticleRepository(db);
        var results = await repository.GetTopByImpactScoreAsync(date, take: 10);

        Assert.Equal([today.Id], results.Select(a => a.Id).ToArray());
    }

    [Fact]
    public async Task GetTopByImpactScoreAsync_ExcludesDuplicateArticles()
    {
        var date = new DateOnly(2026, 1, 25);
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Ranking-Duplicate");

        var canonical = await SeedEnrichedArticleAsync(db, sourceId, "Canonical high-impact story", 85, date);
        var duplicate = await SeedEnrichedArticleAsync(db, sourceId, "Duplicate of high-impact story", 85, date);
        duplicate.DuplicateOfId = canonical.Id;
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        var results = await repository.GetTopByImpactScoreAsync(date, take: 10);

        Assert.Equal([canonical.Id], results.Select(a => a.Id).ToArray());
    }

    [Fact]
    public async Task GetTopByImpactScoreAsync_ExcludesArticlesWithoutCompletedEnrichment()
    {
        var date = new DateOnly(2026, 1, 30);
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Ranking-Unenriched");

        var enriched = await SeedEnrichedArticleAsync(db, sourceId, "Enriched story", 40, date);
        db.Articles.Add(NewArticle(sourceId, "Still-pending enrichment story", "https://example.com/pending-1", date));
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        var results = await repository.GetTopByImpactScoreAsync(date, take: 10);

        Assert.Equal([enriched.Id], results.Select(a => a.Id).ToArray());
    }

    private static DateTimeOffset At(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(6))), TimeSpan.Zero);

    private static Article NewArticle(Guid sourceId, string title, string url, DateOnly publishedDate)
    {
        var published = At(publishedDate);
        return new Article
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            Title = title,
            Url = url,
            RawContent = "Content body for test article.",
            PublishedAt = published,
            FetchedAt = published,
            DedupeHash = Guid.NewGuid().ToString("N"),
        };
    }

    private static async Task<Article> SeedEnrichedArticleAsync(
        ArunikaDbContext db, Guid sourceId, string title, int impactScore, DateOnly publishedDate)
    {
        var article = NewArticle(sourceId, title, $"https://example.com/{Guid.NewGuid():N}", publishedDate);
        article.EnrichmentStatus = EnrichmentStatus.Completed;
        article.Analysis = new ArticleAnalysis
        {
            ArticleId = article.Id,
            Summary = "Test summary.",
            CategoryId = CategoryConfiguration.Markets,
            Sentiment = Sentiment.Neutral,
            SentimentConfidence = 0.5f,
            ImpactScore = impactScore,
            ModelVersion = "test-model",
            GeneratedAt = DateTimeOffset.UtcNow,
        };

        db.Articles.Add(article);
        await db.SaveChangesAsync();
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

