using Arunika.Api.Contracts;
using Arunika.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

/// <summary>
/// Manual recovery endpoint — forces a fetch cycle without waiting for the next
/// Hangfire schedule, e.g. after a Render free-tier instance spins down and
/// misses runs. Gated by a shared secret since there's no admin role system yet;
/// disabled entirely (503) when <c>Admin:TriggerKey</c> isn't configured.
/// </summary>
[ApiController]
[Route("v1/admin")]
public class AdminController(IBackgroundJobClient backgroundJobClient, IConfiguration configuration) : ControllerBase
{
    [HttpPost("jobs/fetch-news")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult TriggerFetchNews([FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        var configuredKey = configuration["Admin:TriggerKey"];
        if (string.IsNullOrEmpty(configuredKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ApiErrorEnvelope(new ApiErrorDetail("ADMIN_DISABLED", "Admin:TriggerKey is not configured.")));
        }

        if (adminKey != configuredKey)
        {
            return Unauthorized(new ApiErrorEnvelope(new ApiErrorDetail("UNAUTHORIZED", "Invalid or missing X-Admin-Key header.")));
        }

        var jobId = backgroundJobClient.Enqueue<FetchNewsJob>(job => job.RunAsync(CancellationToken.None));
        return Accepted(new { jobId });
    }
}
