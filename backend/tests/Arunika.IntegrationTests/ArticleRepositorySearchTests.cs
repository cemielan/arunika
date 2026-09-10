using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Arunika.Infrastructure.Persistence;
using Arunika.Infrastructure.Persistence.Repositories;

namespace Arunika.IntegrationTests;

/// <summary>
/// Exercises the full-text search branch of <see cref="ArticleRepository.GetFeedAsync"/>
/// against a real Postgres instance (Phase 9). The query is a tsvector match rather
/// than a LIKE, so it must be case-insensitive and must tolerate tsquery operator
/// characters typed by a user — behaviour that only shows up against a real Postgres
/// text-search configuration.
/// </summary>
/// <remarks>
/// The Postgres container is shared by the whole collection and rows are never
/// cleaned up between tests, so these assertions check membership of the articles a
/// test seeded itself rather than the total row count.
/// </remarks>
[Collection(PostgresCollection.Name)]
public class ArticleRepositorySearchTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 6, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("disinflation")]
    [InlineData("DISINFLATION")]
    [InlineData("Disinflation")]
    public async Task GetFeedAsync_SearchMatchesTitleRegardlessOfCase(string search)
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, $"Search-Title-{search}");

        var match = NewArticle(
            sourceId,
            "Disinflation arrives faster than economists expected",
            $"https://example.com/search-title-{search}",
            $"hash-search-title-{search}",
            "Consumer prices rose less than forecast last month.");
        db.Articles.Add(match);
        await db.SaveChangesAsync();

        var ids = await SearchIdsAsync(db, search);

        // A LIKE '%term%' would fail the upper- and mixed-case cases outright,
        // because Postgres LIKE is case-sensitive.
        Assert.Contains(match.Id, ids);
    }

    [Fact]
    public async Task GetFeedAsync_SearchMatchesRawContentAndExcludesNonMatches()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Search-Body");

        var match = NewArticle(
            sourceId,
            "Central bank holds policy steady",
            "https://example.com/search-body-match",
            "hash-search-body-match",
            "Officials flagged semiconductor supply as the key risk to the outlook.");
        var nonMatch = NewArticle(
            sourceId,
            "Retail sales beat forecasts",
            "https://example.com/search-body-miss",
            "hash-search-body-miss",
            "Shoppers spent more on groceries and fuel.");
        db.Articles.AddRange(match, nonMatch);
        await db.SaveChangesAsync();

        var ids = await SearchIdsAsync(db, "semiconductor");

        Assert.Contains(match.Id, ids);
        Assert.DoesNotContain(nonMatch.Id, ids);
    }

    [Fact]
    public async Task GetFeedAsync_SearchIsStemmed()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Search-Stemming");

        var match = NewArticle(
            sourceId,
            "Regulator tightens debenture disclosure rules",
            "https://example.com/search-stemming",
            "hash-search-stemming",
            "The change takes effect next quarter.");
        db.Articles.Add(match);
        await db.SaveChangesAsync();

        // "debentures" and "debenture" share a lexeme, so the plural query hits
        // the singular title. Substring matching would miss this.
        Assert.Contains(match.Id, await SearchIdsAsync(db, "debentures"));
    }

    [Fact]
    public async Task GetFeedAsync_SearchWithTsQueryOperators_TreatsThemAsPlainWords()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Search-Operators");

        var match = NewArticle(
            sourceId,
            "Markets shrug off eurobond decision",
            "https://example.com/search-operators",
            "hash-search-operators",
            "Equities closed flat.");
        db.Articles.Add(match);
        await db.SaveChangesAsync();

        // plainto_tsquery must strip these rather than parse them as tsquery
        // syntax; to_tsquery would raise a syntax error and 500 the request.
        Assert.Contains(match.Id, await SearchIdsAsync(db, "eurobond & ! | :*"));
    }

    private static async Task<IReadOnlyList<Guid>> SearchIdsAsync(ArunikaDbContext db, string search)
    {
        var repository = new ArticleRepository(db);
        var (items, _) = await repository.GetFeedAsync(
            category: null, from: null, to: null, sortBy: null, page: 1, pageSize: 100, search: search);
        return items.Select(article => article.Id).ToList();
    }

    private static Article NewArticle(Guid sourceId, string title, string url, string dedupeHash, string rawContent) => new()
    {
        Id = Guid.NewGuid(),
        SourceId = sourceId,
        Title = title,
        Url = url,
        RawContent = rawContent,
        PublishedAt = Now,
        FetchedAt = Now,
        DedupeHash = dedupeHash,
    };

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
