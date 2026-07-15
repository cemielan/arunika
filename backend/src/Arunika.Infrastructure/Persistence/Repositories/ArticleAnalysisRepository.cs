using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence.Repositories;

public class ArticleAnalysisRepository(ArunikaDbContext dbContext) : IArticleAnalysisRepository
{
    public Task<Article?> GetArticleForEnrichmentAsync(Guid articleId, CancellationToken cancellationToken = default)
        => dbContext.Articles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

    public async Task SaveAnalysisAsync(Guid articleId, ArticleAnalysisResult result, CancellationToken cancellationToken = default)
    {
        var article = await dbContext.Articles.FirstAsync(a => a.Id == articleId, cancellationToken);

        var categoryId = await dbContext.Categories
            .Where(c => c.Name == result.Category)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (categoryId is null)
        {
            throw new InvalidOperationException($"Unknown category '{result.Category}' returned by AI enrichment.");
        }

        dbContext.ArticleAnalyses.Add(new ArticleAnalysis
        {
            ArticleId = articleId,
            Summary = result.Summary,
            CategoryId = categoryId.Value,
            Sentiment = result.Sentiment,
            SentimentConfidence = result.SentimentConfidence,
            ImpactScore = result.ImpactScore,
            ImpactRationale = result.ImpactRationale,
            ModelVersion = result.ModelVersion,
            GeneratedAt = DateTimeOffset.UtcNow
        });

        foreach (var sector in result.Sectors)
        {
            var sectorId = await dbContext.Sectors
                .Where(s => s.Name == sector.Sector)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (sectorId is null)
            {
                // Unknown sector name from the model (shouldn't happen given the schema's
                // free-text sector field) — skip it rather than failing the whole enrichment.
                continue;
            }

            dbContext.ArticleSectorImpacts.Add(new ArticleSectorImpact
            {
                ArticleId = articleId,
                SectorId = sectorId.Value,
                Direction = sector.Direction,
                Magnitude = sector.Magnitude
            });
        }

        foreach (var keywordText in result.Keywords.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var keyword = await dbContext.Keywords.FirstOrDefaultAsync(k => k.Text == keywordText, cancellationToken);
            if (keyword is null)
            {
                keyword = new Keyword { Id = Guid.NewGuid(), Text = keywordText };
                dbContext.Keywords.Add(keyword);
            }

            dbContext.ArticleKeywords.Add(new ArticleKeyword { ArticleId = articleId, KeywordId = keyword.Id });
        }

        article.EnrichmentStatus = EnrichmentStatus.Completed;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkEnrichmentFailedAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var article = await dbContext.Articles.FirstAsync(a => a.Id == articleId, cancellationToken);
        article.EnrichmentStatus = EnrichmentStatus.Failed;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetFailedArticleIdsAsync(int maxCount, CancellationToken cancellationToken = default)
        => await dbContext.Articles
            .Where(a => a.EnrichmentStatus == EnrichmentStatus.Failed)
            .OrderBy(a => a.FetchedAt)
            .Take(maxCount)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
}
