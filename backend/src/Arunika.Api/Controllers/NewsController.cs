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
    /// Lists non-duplicate articles, newest first, optionally filtered by category.
    /// </summary>
    /// <param name="category">Exact category name (e.g. "Markets"). Omit for all categories.</param>
    /// <param name="page">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Items per page (1-100). Defaults to 20.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The requested page of the news feed.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NewsListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNews(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;

        var (items, totalItems) = await articleRepository.GetFeedAsync(category, page, pageSize, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var data = items.Select(article => article.ToListItemDto()).ToList();
        var meta = new PageMeta(page, pageSize, totalItems, totalPages);

        return Ok(new ApiResponse<IReadOnlyList<NewsListItemDto>>(data, meta));
    }
}
