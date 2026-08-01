using System.Security.Claims;
using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

[ApiController]
[Route("v1/users")]
[Produces("application/json")]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(idClaim) || !Guid.TryParse(idClaim, out var userId))
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(
                "INVALID_TOKEN", "Token is missing a valid user identity.")));
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new ApiErrorEnvelope(new ApiErrorDetail(
                "USER_NOT_FOUND", "No profile exists for this account.")));
        }

        return Ok(new ApiResponse<MeResponse>(new MeResponse(user.Id, user.Email, user.Role)));
    }

    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpsertMe(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(idClaim) || !Guid.TryParse(idClaim, out var userId))
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(
                "INVALID_TOKEN", "Token is missing a valid user identity.")));
        }

        if (string.IsNullOrWhiteSpace(emailClaim))
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(
                "INVALID_TOKEN", "Token is missing the user email.")));
        }

        await userRepository.UpsertAsync(new User
        {
            Id = userId,
            Email = emailClaim,
            Role = "user",
            EmailVerified = true,
        }, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<MeResponse>(new MeResponse(userId, emailClaim, "user")));
    }
}
