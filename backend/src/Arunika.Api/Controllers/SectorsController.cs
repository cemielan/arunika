using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Arunika.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

[ApiController]
[Route("v1/sectors")]
[Produces("application/json")]
public class SectorsController(IArticleRepository articleRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SectorAggregationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectorAggregation(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-7);

        var articles = await articleRepository.GetEnrichedArticlesInRangeAsync(from, now, cancellationToken);

        var sectorGroups = articles
            .Where(a => a.SectorImpacts.Count != 0)
            .SelectMany(a => a.SectorImpacts, (article, impact) => new
            {
                Sector = impact.Sector?.Name ?? "Unknown",
                Impact = article.Analysis!.ImpactScore,
                Sentiment = article.Analysis!.Sentiment,
            })
            .GroupBy(s => s.Sector)
            .Select(g =>
            {
                var total = g.Count();
                var bullish = g.Count(s => s.Sentiment == Sentiment.Bullish);
                var bearish = g.Count(s => s.Sentiment == Sentiment.Bearish);
                var neutral = g.Count(s => s.Sentiment == Sentiment.Neutral);

                var dominant = "Neutral";
                if (bullish > bearish && bullish > neutral) dominant = "Bullish";
                else if (bearish > bullish && bearish > neutral) dominant = "Bearish";

                return new SectorAggregationDto(
                    g.Key,
                    total,
                    (int)Math.Round(g.Average(s => s.Impact)),
                    bullish, bearish, neutral, dominant);
            })
            .OrderByDescending(s => s.ArticleCount)
            .ToList();

        return Ok(new ApiResponse<IReadOnlyList<SectorAggregationDto>>(sectorGroups));
    }
}
