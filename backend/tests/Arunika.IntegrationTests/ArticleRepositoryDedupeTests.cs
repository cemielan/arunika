using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Arunika.Infrastructure.Persistence;
using Arunika.Infrastructure.Persistence.Repositories;

namespace Arunika.IntegrationTests;

/// <summary>
/// Exercises <see cref="ArticleRepository.FindDuplicateAsync"/> against a real
/// Postgres instance (Phase 4 dedup logic, design doc §6): exact dedupe-hash
/// matches, near-duplicate title matches via pg_trgm trigram similarity, and
/// the cases that must NOT be treated as duplicates.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ArticleRepositoryDedupeTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FindDuplicateAsync_ExactDedupeHashMatch_ReturnsTheEarlierArticle()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Reuters-Exact");

        var original = NewArticle(
            sourceId,
            "Fed keeps interest rates unchanged",
            "https://example.com/exact-1",
            "hash-exact-match",
            Now.AddHours(-1));
        db.Articles.Add(original);
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        var duplicate = await repository.FindDuplicateAsync("hash-exact-match", "Fed keeps interest rates unchanged", Now.AddHours(-48));

        Assert.NotNull(duplicate);
        Assert.Equal(original.Id, duplicate!.Id);
    }

    [Fact]
    public async Task FindDuplicateAsync_SimilarTitleFromAnotherOutlet_ReturnsCanonicalArticle()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "AP-Similar");

        var canonical = NewArticle(
            sourceId,
            "Tesla beats quarterly earnings expectations by wide margin",
            "https://example.com/similar-1",
            "hash-canonical-similar",
            Now.AddHours(-2));
        db.Articles.Add(canonical);
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        // Near-verbatim wire-copy rewrite from a different outlet — different
        // hash (different source name), but highly similar title text.
        var duplicate = await repository.FindDuplicateAsync(
            "hash-different-outlet-similar",
            "Tesla beats quarterly earnings expectations by a wide margin",
            Now.AddHours(-48));

        Assert.NotNull(duplicate);
        Assert.Equal(canonical.Id, duplicate!.Id);
    }

    [Fact]
    public async Task FindDuplicateAsync_UnrelatedTitle_ReturnsNull()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Reuters-Unrelated");

        db.Articles.Add(NewArticle(
            sourceId,
            "Japan GDP slows amid weaker exports",
            "https://example.com/unrelated-1",
            "hash-unrelated",
            Now.AddHours(-1)));
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        var duplicate = await repository.FindDuplicateAsync(
            "hash-completely-different",
            "Oil prices rise four percent on supply concerns",
            Now.AddHours(-48));

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task FindDuplicateAsync_MatchOutsideTheSinceWindow_ReturnsNull()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Reuters-Stale");

        db.Articles.Add(NewArticle(
            sourceId,
            "Indonesia changes export policy for nickel ore",
            "https://example.com/stale-1",
            "hash-stale-match",
            Now.AddHours(-72)));
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        var duplicate = await repository.FindDuplicateAsync(
            "hash-stale-match",
            "Indonesia changes export policy for nickel ore",
            Now.AddHours(-48));

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task FindDuplicateAsync_NeverMatchesAnArticleThatIsItselfADuplicate()
    {
        await using var db = fixture.CreateDbContext();
        var sourceId = await SeedSourceAsync(db, "Reuters-Chain");

        var canonical = NewArticle(
            sourceId,
            "Bank of Japan holds rates steady at policy meeting",
            "https://example.com/chain-1",
            "hash-chain-canonical",
            Now.AddHours(-3));
        db.Articles.Add(canonical);
        await db.SaveChangesAsync();

        var firstDuplicate = NewArticle(
            sourceId,
            "Bank of Japan holds rates steady at policy meeting",
            "https://example.com/chain-2",
            "hash-chain-duplicate",
            Now.AddHours(-2));
        firstDuplicate.DuplicateOfId = canonical.Id;
        db.Articles.Add(firstDuplicate);
        await db.SaveChangesAsync();

        var repository = new ArticleRepository(db);
        var duplicate = await repository.FindDuplicateAsync(
            "hash-chain-duplicate",
            "Bank of Japan holds rates steady at policy meeting",
            Now.AddHours(-48));

        // Must resolve to the original canonical article, never to the
        // already-linked duplicate row (design doc §4: duplicates never chain).
        Assert.NotNull(duplicate);
        Assert.Equal(canonical.Id, duplicate!.Id);
    }

    private static Article NewArticle(Guid sourceId, string title, string url, string dedupeHash, DateTimeOffset publishedAt) => new()
    {
        Id = Guid.NewGuid(),
        SourceId = sourceId,
        Title = title,
        Url = url,
        RawContent = "Content body for test article.",
        PublishedAt = publishedAt,
        FetchedAt = publishedAt,
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
