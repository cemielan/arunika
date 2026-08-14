using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
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

    [HttpPost("jobs/fetch-news/sync")]
    [ProducesResponseType(typeof(FetchNewsJob.FetchNewsRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TriggerFetchNewsSync(
        [FromServices] FetchNewsJob fetchNewsJob,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        CancellationToken cancellationToken)
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

        var report = await fetchNewsJob.RunWithReportAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Sends a single test email to an arbitrary address so SMTP configuration
    /// can be verified in isolation from the digest pipeline.
    /// </summary>
    [HttpPost("email/test")]
    [ProducesResponseType(typeof(SmtpTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SendTestEmail(
        [FromServices] IEmailService emailService,
        [FromBody] SendTestEmailRequest request,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(request.To))
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail("VALIDATION_ERROR", "'to' is required.")));
        }

        try
        {
            await emailService.SendAsync(
                request.To,
                "Arunika SMTP Test",
                "<p>This is a test email from the Arunika backend. SMTP is working.</p>",
                cancellationToken);
            return Ok(new SmtpTestResult(true, request.To, null));
        }
        catch (Exception ex)
        {
            return Ok(new SmtpTestResult(false, request.To, ex.Message));
        }
    }

    [HttpPost("jobs/send-digest")]
    [ProducesResponseType(typeof(DigestSendResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TriggerSendDigest(
        [FromServices] SendEmailDigestJob sendEmailDigestJob,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        CancellationToken cancellationToken)
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

        var report = await sendEmailDigestJob.RunWithReportAsync(cancellationToken);
        return Ok(new DigestSendResult(report.Sent, report.Failed, report.Error));
    }
}

public sealed record SendTestEmailRequest(string To);

public sealed record SmtpTestResult(bool Ok, string To, string? Error);

public sealed record DigestSendResult(int Sent, int Failed, string? Error);
