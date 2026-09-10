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
            query = query.Where(a => a.Title.Contains(search) || a.RawContent.Contains(search));
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
            // Ties have to break on something stable. Ordering on the score alone
            // leaves tied rows in whatever order the query plan produces, and the
            // briefing then takes the top ten off that list — so which stories made
            // the cut could change between two requests over unchanged data.
            .OrderByDescending(a => a.Analysis!.ImpactScore)
            .ThenByDescending(a => a.PublishedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = olderThan.UtcDateTime;

        var oldArticles = await dbContext.Articles
            .Where(a => a.PublishedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (oldArticles.Count == 0)
        {
            return 0;
        }

        var ids = oldArticles.Select(a => a.Id).ToList();

        await dbContext.ArticleSectorImpacts
            .Where(si => ids.Contains(si.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ArticleKeywords
            .Where(ak => ids.Contains(ak.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.BriefingItems
            .Where(bi => ids.Contains(bi.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ArticleAnalyses
            .Where(aa => ids.Contains(aa.ArticleId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Articles
            .Where(a => ids.Contains(a.DuplicateOfId!.Value))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Articles
            .Where(a => ids.Contains(a.Id))
            .ExecuteDeleteAsync(cancellationToken);

        return oldArticles.Count;
    }
}
