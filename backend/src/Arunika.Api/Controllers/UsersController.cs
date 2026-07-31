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
            PasswordHash = string.Empty,
            Role = "user",
            EmailVerified = true,
        }, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<MeResponse>(new MeResponse(userId, emailClaim, "user")));
    }
}
