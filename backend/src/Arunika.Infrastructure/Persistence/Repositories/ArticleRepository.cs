using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence.Repositories;

public class ArticleRepository(ArunikaDbContext dbContext) : IArticleRepository
{
    public Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
        => dbContext.Articles.FirstOrDefaultAsync(a => a.Url == url, cancellationToken);

    public async Task<IReadOnlyList<Article>> GetRecentForDedupeCheckAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
        => await dbContext.Articles
            .Where(a => a.PublishedAt >= since)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
        => await dbContext.Articles.AddAsync(article, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
