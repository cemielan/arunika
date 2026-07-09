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
}
