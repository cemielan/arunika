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
        try
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

            // Enrichment can legitimately be re-run for the same article (concurrent Hangfire
            // retries, or RetryFailedEnrichmentJob re-processing it) — wipe any prior analysis
            // first so this is an idempotent upsert instead of failing PK_article_analyses.
            await dbContext.ArticleSectorImpacts.Where(x => x.ArticleId == articleId).ExecuteDeleteAsync(cancellationToken);
            await dbContext.ArticleKeywords.Where(x => x.ArticleId == articleId).ExecuteDeleteAsync(cancellationToken);
            await dbContext.ArticleAnalyses.Where(x => x.ArticleId == articleId).ExecuteDeleteAsync(cancellationToken);

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
            article.EnrichmentRetryCount = 0;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Drop any entities tracked from the failed attempt above so a subsequent call on
            // this same (scoped) DbContext instance — e.g. MarkEnrichmentFailedAsync in the
            // caller's catch block — doesn't try to re-save them and fail a second time.
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task MarkEnrichmentFailedAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        await dbContext.Articles
            .Where(a => a.Id == articleId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.EnrichmentStatus, EnrichmentStatus.Failed)
                .SetProperty(a => a.EnrichmentRetryCount, a => a.EnrichmentRetryCount + 1), cancellationToken);
    }

    public async Task<int> GetEnrichmentRetryCountAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.Articles
            .Where(a => a.Id == articleId)
            .Select(a => a.EnrichmentRetryCount)
            .FirstOrDefaultAsync(cancellationToken);
        return count;
    }

    public async Task<IReadOnlyList<Guid>> GetFailedArticleIdsAsync(int maxCount, CancellationToken cancellationToken = default)
        => await dbContext.Articles
            .Where(a => a.EnrichmentStatus == EnrichmentStatus.Failed)
            .OrderBy(a => a.FetchedAt)
            .Take(maxCount)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task SaveBriefingAsync(Briefing briefing, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Briefings
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.BriefingDate == briefing.BriefingDate, cancellationToken);

        if (existing is not null)
        {
            dbContext.BriefingItems.RemoveRange(existing.Items);
            dbContext.Briefings.Remove(existing);
        }

        dbContext.Briefings.Add(briefing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Briefing?> GetBriefingByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => dbContext.Briefings
            .Include(b => b.Items.OrderBy(i => i.Rank))
                .ThenInclude(bi => bi.Article!)
                    .ThenInclude(a => a.Analysis)
            .FirstOrDefaultAsync(b => b.BriefingDate == date, cancellationToken);

    public Task<Briefing?> GetLatestBriefingAsync(CancellationToken cancellationToken = default)
        => dbContext.Briefings
            .Include(b => b.Items.OrderBy(i => i.Rank))
                .ThenInclude(bi => bi.Article!)
                    .ThenInclude(a => a.Analysis)
            .OrderByDescending(b => b.BriefingDate)
            .FirstOrDefaultAsync(cancellationToken);
}
