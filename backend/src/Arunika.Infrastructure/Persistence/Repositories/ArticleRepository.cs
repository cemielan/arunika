using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence.Repositories;

public class ArticleRepository(ArunikaDbContext dbContext) : IArticleRepository
{
    // Below this similarity score, two titles are treated as unrelated stories
    // rather than the same story from different outlets (design doc §6, step 2).
    private const double TitleSimilarityThreshold = 0.5;

    public Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        => dbContext.Articles.FirstOrDefaultAsync(a => a.Url == url, cancellationToken);

    public async Task<Article?> FindDuplicateAsync(string dedupeHash, string title, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var exactMatch = await dbContext.Articles
            .Where(a => a.DuplicateOfId == null && a.PublishedAt >= since && a.DedupeHash == dedupeHash)
            .OrderBy(a => a.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        return await dbContext.Articles
            .Where(a => a.DuplicateOfId == null && a.PublishedAt >= since)
            .Where(a => EF.Functions.TrigramsSimilarity(a.Title, title) >= TitleSimilarityThreshold)
            .OrderByDescending(a => EF.Functions.TrigramsSimilarity(a.Title, title))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
        => await dbContext.Articles.AddAsync(article, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<(IReadOnlyList<Article> Items, int TotalItems)> GetFeedAsync(
        string? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? sortBy,
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Articles
            .Where(a => a.DuplicateOfId == null)
            .Include(a => a.Source)
            .Include(a => a.Analysis!)
                .ThenInclude(analysis => analysis.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Analysis != null && a.Analysis.Category != null && a.Analysis.Category.Name == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Full-text match against the GIN tsvector index on (title, raw_content)
            // declared in ArticleConfiguration. Written as to_tsvector(title || ' ' ||
            // raw_content) so Postgres can use that index; a LIKE '%term%' here would
            // both bypass the index and be case-sensitive.
            // plainto_tsquery treats the input as plain words, so tsquery operators
            // (& | ! :*) typed by a user are escaped rather than causing a syntax error.
            query = query.Where(a => EF.Functions
                .ToTsVector("english", a.Title + " " + a.RawContent)
                .Matches(EF.Functions.PlainToTsQuery("english", search)));
        }

        if (from is not null)
        {
            query = query.Where(a => a.PublishedAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(a => a.PublishedAt < to.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        query = string.Equals(sortBy, "impact", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(a => a.Analysis != null ? a.Analysis.ImpactScore : -1).ThenByDescending(a => a.PublishedAt)
            : query.OrderByDescending(a => a.PublishedAt);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<Article?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Articles
            .Include(a => a.Source)
            .Include(a => a.Analysis!)
                .ThenInclude(analysis => analysis.Category)
            .Include(a => a.SectorImpacts)
                .ThenInclude(impact => impact.Sector)
            .Include(a => a.Keywords)
                .ThenInclude(articleKeyword => articleKeyword.Keyword)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Article>> GetDuplicatesOfAsync(Guid canonicalArticleId, CancellationToken cancellationToken = default)
        => await dbContext.Articles
            .Where(a => a.DuplicateOfId == canonicalArticleId)
            .Include(a => a.Source)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Article>> GetTopByImpactScoreAsync(DateOnly date, int take, CancellationToken cancellationToken = default)
    {
        var startOfDay = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        return await dbContext.Articles
            .Where(a => a.DuplicateOfId == null && a.Analysis != null)
            .Where(a => a.PublishedAt >= startOfDay && a.PublishedAt < endOfDay)
            .Include(a => a.Analysis)
            .Include(a => a.SectorImpacts)
                .ThenInclude(impact => impact.Sector)
            .OrderByDescending(a => a.Analysis!.ImpactScore)
            .ThenByDescending(a => a.PublishedAt)
            .ThenBy(a => a.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Article>> GetEnrichedArticlesInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        => await dbContext.Articles
            .Where(a => a.DuplicateOfId == null && a.Analysis != null)
            .Where(a => a.PublishedAt >= from && a.PublishedAt < to)
            .Include(a => a.Analysis)
                .ThenInclude(analysis => analysis!.Category)
            .Include(a => a.Source)
            .Include(a => a.SectorImpacts)
                .ThenInclude(impact => impact.Sector)
            // Keywords group same-event stories in TopStorySelector.
            .Include(a => a.Keywords)
                .ThenInclude(k => k.Keyword)
            .AsSplitQuery()
            // Ties have to break on something stable. Ordering on the score alone
            // leaves tied rows in whatever order the query plan produces, and the
            // briefing then takes the top ten off that list — so which stories made
            // the cut could change between two requests over unchanged data.
            .OrderByDescending(a => a.Analysis!.ImpactScore)
            .ThenByDescending(a => a.PublishedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetSourceCountsAsync(IReadOnlyCollection<Guid> articleIds, CancellationToken cancellationToken = default)
    {
        if (articleIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var storySources = await dbContext.Articles
            .Where(a => articleIds.Contains(a.Id) || (a.DuplicateOfId != null && articleIds.Contains(a.DuplicateOfId.Value)))
            .Select(a => new { Story = a.DuplicateOfId ?? a.Id, a.SourceId })
            .Distinct()
            .ToListAsync(cancellationToken);

        return storySources
            .GroupBy(x => x.Story)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = olderThan.UtcDateTime;

        // A briefing links its ranked stories by id, so deleting one leaves an
        // archived briefing with a hole in its ranking. Those articles, and the
        // duplicates that make up their "also reported by" coverage, are kept
        // for good — at most ten a day.
        var ids = await dbContext.Articles
            .Where(a => a.PublishedAt < cutoff)
            .Where(a => !a.BriefingItems.Any())
            .Where(a => a.DuplicateOf == null || !a.DuplicateOf.BriefingItems.Any())
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        // Each delete would otherwise commit on its own, and a failure part way
        // through would leave articles stripped of their analysis until the
        // next night's run.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.ArticleSectorImpacts
            .Where(si => ids.Contains(si.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ArticleKeywords
            .Where(ak => ids.Contains(ak.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ArticleAnalyses
            .Where(aa => ids.Contains(aa.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        // Duplicates of a deleted story can be newer than the cutoff; they go
        // with it, since the foreign key won't let them outlive it.
        await dbContext.Articles
            .Where(a => ids.Contains(a.DuplicateOfId!.Value))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Articles
            .Where(a => ids.Contains(a.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ids.Count;
    }
}
