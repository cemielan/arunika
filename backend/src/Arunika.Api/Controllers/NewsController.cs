using Arunika.Api.Contracts;
using Arunika.Api.Mapping;
using Arunika.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

/// <summary>
/// <c>GET /v1/news</c> — the filterable article feed (design doc §7).
/// </summary>
[ApiController]
[Route("v1/news")]
[Produces("application/json")]
public class NewsController(IArticleRepository articleRepository) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    /// <summary>
    /// Lists non-duplicate articles, optionally filtered by category and/or published-date
    /// range, sorted newest-first or by impact score.
    /// </summary>
    /// <param name="category">Exact category name (e.g. "Markets"). Omit for all categories.</param>
    /// <param name="from">Only include articles published on/after this date (UTC, inclusive). Omit for no lower bound.</param>
    /// <param name="to">Only include articles published on/before this date (UTC, inclusive). Omit for no upper bound.</param>
    /// <param name="sortBy">"date" (default, newest first) or "impact" (highest impact score first).</param>
    /// <param name="page">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Items per page (1-100). Defaults to 20.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The requested page of the news feed.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NewsListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNews(
        [FromQuery] string? category,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;

        var fromInclusive = from is null ? (DateTimeOffset?)null : new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toExclusive = to is null ? (DateTimeOffset?)null : new DateTimeOffset(to.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);

        var (items, totalItems) = await articleRepository.GetFeedAsync(
            category, fromInclusive, toExclusive, sortBy, page, pageSize, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var data = items.Select(article => article.ToListItemDto()).ToList();
        var meta = new PageMeta(page, pageSize, totalItems, totalPages);

        return Ok(new ApiResponse<IReadOnlyList<NewsListItemDto>>(data, meta));
    }
}
