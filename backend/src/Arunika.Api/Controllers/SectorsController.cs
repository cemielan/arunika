using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Arunika.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

[ApiController]
[Route("v1/sectors")]
[Produces("application/json")]
public class SectorsController(
    IArticleRepository articleRepository,
    ISectorStatsRepository sectorStatsRepository) : ControllerBase
{
    private const int DefaultHistoryDays = 30;
    private const int MaxHistoryDays = 365;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SectorAggregationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectorAggregation(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-7);

        var articles = await articleRepository.GetEnrichedArticlesInRangeAsync(from, now, cancellationToken);

        var sectorGroups = SentimentTally.BySector(articles)
            .Select(s => new SectorAggregationDto(
                s.Sector,
                s.Tally.Total,
                s.Tally.AverageImpactScore,
                s.Tally.BullishCount,
                s.Tally.BearishCount,
                s.Tally.NeutralCount,
                s.Tally.Dominant.ToString()))
            .OrderByDescending(s => s.ArticleCount)
            .ToList();

        return Ok(new ApiResponse<IReadOnlyList<SectorAggregationDto>>(sectorGroups));
    }

    /// <summary>
    /// Daily per-sector figures for the last <paramref name="days"/> days, oldest
    /// first. Snapshotted nightly by <c>CleanupOldArticlesJob</c>, so today and
    /// days before the first snapshot have no rows.
    /// </summary>
    /// <param name="days">How far back to go (1-365). Defaults to 30.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SectorHistoryPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectorHistory([FromQuery] int days = DefaultHistoryDays, CancellationToken cancellationToken = default)
    {
        days = days is < 1 or > MaxHistoryDays ? DefaultHistoryDays : days;
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);

        var stats = await sectorStatsRepository.GetSinceAsync(from, cancellationToken);

        var data = stats
            .Select(s => new SectorHistoryPointDto(
                s.Date,
                s.Sector?.Name ?? "Unknown",
                s.ArticleCount,
                s.AverageImpactScore,
                s.BullishCount,
                s.BearishCount,
                s.NeutralCount))
            .ToList();

        return Ok(new ApiResponse<IReadOnlyList<SectorHistoryPointDto>>(data));
    }
}
