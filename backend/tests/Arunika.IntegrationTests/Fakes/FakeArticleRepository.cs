using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;

namespace Arunika.IntegrationTests.Fakes;

/// <summary>
/// In-memory <see cref="IArticleRepository"/> used to exercise the Phase 6
/// controllers end-to-end (routing, envelope shape, status codes) without a
/// real Postgres instance. The write-side members (dedup/save) aren't
/// exercised by these read-only API tests and simply throw if ever called.
/// </summary>
public class FakeArticleRepository : IArticleRepository
{
    public const string MarketsCategory = "Markets";
    public const string TechnologyCategory = "Technology";

    public static readonly Guid MarketsArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid TechArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid DuplicateArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    public static readonly Guid UnenrichedArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");

    private readonly List<Article> _articles;

    public FakeArticleRepository()
    {
        var reuters = new NewsSource { Id = Guid.NewGuid(), Name = "Reuters", BaseUrl = "https://reuters.com", SourceType = SourceType.Rss, TrustScore = 90 };
        var cnbc = new NewsSource { Id = Guid.NewGuid(), Name = "CNBC", BaseUrl = "https://cnbc.com", SourceType = SourceType.Rss, TrustScore = 80 };

        var marketsCategory = new Category { Id = Guid.NewGuid(), Name = MarketsCategory };
        var technologyCategory = new Category { Id = Guid.NewGuid(), Name = TechnologyCategory };

        var financials = new Sector { Id = Guid.NewGuid(), Name = "Financials" };
        var technology = new Sector { Id = Guid.NewGuid(), Name = "Technology" };

        var now = DateTimeOffset.UtcNow;

        var marketsArticle = new Article
        {
            Id = MarketsArticleId,
            SourceId = reuters.Id,
            Source = reuters,
            Title = "Fed keeps interest rates unchanged",
            Url = "https://reuters.com/fed-rates",
            RawContent = "The Federal Reserve held its benchmark rate steady...",
            PublishedAt = now.AddHours(-1),
            FetchedAt = now.AddHours(-1),
            DedupeHash = "hash-1",
            EnrichmentStatus = EnrichmentStatus.Completed
        };
        marketsArticle.Analysis = new ArticleAnalysis
        {
            ArticleId = marketsArticle.Id,
            Summary = "The Federal Reserve held its benchmark rate steady amid mixed signals.",
            CategoryId = marketsCategory.Id,
            Category = marketsCategory,
            Sentiment = Sentiment.Neutral,
            SentimentConfidence = 0.8f,
            ImpactScore = 82,
            ImpactRationale = "High market significance due to rate policy implications.",
            ModelVersion = "gemini-3.1-flash-lite",
            GeneratedAt = now.AddHours(-1)
        };
        marketsArticle.SectorImpacts.Add(new ArticleSectorImpact
        {
            ArticleId = marketsArticle.Id,
            SectorId = financials.Id,
            Sector = financials,
            Direction = ImpactDirection.Positive,
            Magnitude = 70
        });
        var rateKeyword = new Keyword { Id = Guid.NewGuid(), Text = "interest rates" };
        marketsArticle.Keywords.Add(new ArticleKeyword { ArticleId = marketsArticle.Id, KeywordId = rateKeyword.Id, Keyword = rateKeyword });

        var techArticle = new Article
        {
            Id = TechArticleId,
            SourceId = cnbc.Id,
            Source = cnbc,
            Title = "Tesla beats earnings",
            Url = "https://cnbc.com/tesla-earnings",
            RawContent = "Tesla reported quarterly earnings above analyst expectations...",
            PublishedAt = now.AddHours(-2),
            FetchedAt = now.AddHours(-2),
            DedupeHash = "hash-2",
            EnrichmentStatus = EnrichmentStatus.Completed
        };
        techArticle.Analysis = new ArticleAnalysis
        {
            ArticleId = techArticle.Id,
            Summary = "Tesla's quarterly results beat Wall Street expectations.",
            CategoryId = technologyCategory.Id,
            Category = technologyCategory,
            Sentiment = Sentiment.Bullish,
            SentimentConfidence = 0.9f,
            ImpactScore = 65,
            ImpactRationale = "Moderate significance for the technology sector.",
            ModelVersion = "gemini-3.1-flash-lite",
            GeneratedAt = now.AddHours(-2)
        };
        techArticle.SectorImpacts.Add(new ArticleSectorImpact
        {
            ArticleId = techArticle.Id,
            SectorId = technology.Id,
            Sector = technology,
            Direction = ImpactDirection.Positive,
            Magnitude = 60
        });

        var duplicateArticle = new Article
        {
            Id = DuplicateArticleId,
            SourceId = cnbc.Id,
            Source = cnbc,
            Title = "Fed keeps interest rates unchanged (wire)",
            Url = "https://cnbc.com/fed-rates-wire",
            RawContent = "Wire copy of the Fed rate decision...",
            PublishedAt = now.AddMinutes(-50),
            FetchedAt = now.AddMinutes(-50),
            DedupeHash = "hash-1",
            DuplicateOfId = marketsArticle.Id,
            EnrichmentStatus = EnrichmentStatus.Pending
        };

        var unenrichedArticle = new Article
        {
            Id = UnenrichedArticleId,
            SourceId = reuters.Id,
            Source = reuters,
            Title = "Japan GDP slows",
            Url = "https://reuters.com/japan-gdp",
            RawContent = "Japan's economy grew slower than expected...",
            PublishedAt = now.AddMinutes(-10),
            FetchedAt = now.AddMinutes(-10),
            DedupeHash = "hash-3",
            EnrichmentStatus = EnrichmentStatus.Pending
        };

        _articles = [marketsArticle, techArticle, duplicateArticle, unenrichedArticle];
    }

    public Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task<Article?> FindDuplicateAsync(string dedupeHash, string title, DateTimeOffset since, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task AddAsync(Article article, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task<(IReadOnlyList<Article> Items, int TotalItems)> GetFeedAsync(
        string? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _articles.Where(a => a.DuplicateOfId == null);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Analysis?.Category?.Name == category);
        }

        if (from is not null)
        {
            query = query.Where(a => a.PublishedAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(a => a.PublishedAt < to.Value);
        }

        var ordered = string.Equals(sortBy, "impact", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(a => a.Analysis?.ImpactScore ?? -1).ToList()
            : query.OrderByDescending(a => a.PublishedAt).ToList();

        var totalItems = ordered.Count;
        var page1Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult<(IReadOnlyList<Article>, int)>((page1Items, totalItems));
    }

    public Task<Article?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_articles.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<Article>> GetDuplicatesOfAsync(Guid canonicalArticleId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Article>>(_articles.Where(a => a.DuplicateOfId == canonicalArticleId).ToList());

    public Task<IReadOnlyList<Article>> GetTopByImpactScoreAsync(DateOnly date, int take, CancellationToken cancellationToken = default)
    {
        var results = _articles
            .Where(a => a.DuplicateOfId == null && a.Analysis is not null)
            .Where(a => DateOnly.FromDateTime(a.PublishedAt.UtcDateTime) == date)
            .OrderByDescending(a => a.Analysis!.ImpactScore)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<Article>>(results);
    }

    public Task<IReadOnlyList<Article>> GetEnrichedArticlesInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var results = _articles
            .Where(a => a.DuplicateOfId == null && a.Analysis is not null)
            .Where(a => a.PublishedAt >= from && a.PublishedAt < to)
            .OrderByDescending(a => a.Analysis!.ImpactScore)
            .ToList();

        return Task.FromResult<IReadOnlyList<Article>>(results);
    }
}
