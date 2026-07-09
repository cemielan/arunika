using Arunika.Api.Contracts;
using Arunika.Api.Mapping;
using Arunika.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

/// <summary>
/// <c>GET /v1/articles/{id}</c> — full article detail (design doc §7).
/// </summary>
[ApiController]
[Route("v1/articles")]
[Produces("application/json")]
public class ArticlesController(IArticleRepository articleRepository) : ControllerBase
{
    /// <summary>
    /// Returns the full detail for one article: summary, sentiment, impact
    /// rationale, sector breakdown, keywords, and any articles linked as
    /// duplicates of it.
    /// </summary>
    /// <response code="200">The article detail.</response>
    /// <response code="404">No article exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(Guid id, CancellationToken cancellationToken)
    {
        var article = await articleRepository.GetDetailByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound(new ApiErrorEnvelope(new ApiErrorDetail("NOT_FOUND", "Article not found")));
        }

        var duplicates = await articleRepository.GetDuplicatesOfAsync(id, cancellationToken);

        return Ok(new ApiResponse<ArticleDetailDto>(article.ToDetailDto(duplicates)));
    }
}
