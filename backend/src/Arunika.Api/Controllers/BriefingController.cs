using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

/// <summary>
/// <c>GET /v1/briefing</c> — daily morning brief (design doc §7). V1 is a
/// simple version per the to-do list: top stories ranked by impact score for
/// the day, computed on request. The AI-generated executive
/// summary/overall sentiment/risk level/watchlist arrive with
/// <c>GenerateDailyBriefingJob</c> (Phase 9).
/// </summary>
[ApiController]
[Route("v1/briefing")]
[Produces("application/json")]
public class BriefingController(IArticleRepository articleRepository) : ControllerBase
{
    private const int TopStoryCount = 10;

    /// <summary>
    /// Returns the top stories by impact score for the given date (defaults to today, UTC).
    /// </summary>
    /// <param name="date">The briefing date. Defaults to today (UTC).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The briefing for the requested date.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BriefingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBriefing([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var briefingDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var topArticles = await articleRepository.GetTopByImpactScoreAsync(briefingDate, TopStoryCount, cancellationToken);

        var topStories = topArticles
            .Select(article => new TopStoryDto(
                article.Id,
                article.Title,
                article.Analysis!.ImpactScore,
                article.SectorImpacts.Select(impact => impact.Sector?.Name ?? string.Empty).ToList()))
            .ToList();

        var data = new BriefingResponseDto(briefingDate, topStories);
        var meta = new { generatedAt = DateTimeOffset.UtcNow };

        return Ok(new ApiResponse<BriefingResponseDto>(data, meta));
    }
}
