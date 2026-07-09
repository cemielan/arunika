using Arunika.Api.Contracts;
using Arunika.Domain.Entities;

namespace Arunika.Api.Mapping;

public static class ArticleMappingExtensions
{
    public static NewsListItemDto ToListItemDto(this Article article) => new(
        article.Id,
        article.Title,
        article.Source?.Name ?? string.Empty,
        article.PublishedAt,
        article.Analysis?.Category?.Name,
        article.Analysis?.Sentiment.ToString(),
        article.Analysis?.ImpactScore,
        article.Analysis?.Summary);

    public static ArticleDetailDto ToDetailDto(this Article article, IReadOnlyList<Article> duplicates) => new(
        article.Id,
        article.Title,
        article.Url,
        article.Source?.Name ?? string.Empty,
        article.PublishedAt,
        article.EnrichmentStatus.ToString(),
        article.Analysis?.Summary,
        article.Analysis?.Category?.Name,
        article.Analysis?.Sentiment.ToString(),
        article.Analysis?.SentimentConfidence,
        article.Analysis?.ImpactScore,
        article.Analysis?.ImpactRationale,
        article.SectorImpacts
            .Select(impact => new SectorImpactDto(impact.Sector?.Name ?? string.Empty, impact.Direction.ToString(), impact.Magnitude))
            .ToList(),
        article.Keywords
            .Select(articleKeyword => articleKeyword.Keyword?.Text ?? string.Empty)
            .ToList(),
        article.DuplicateOfId,
        duplicates
            .Select(duplicate => new DuplicateArticleDto(duplicate.Id, duplicate.Title, duplicate.Source?.Name ?? string.Empty))
            .ToList());
}
