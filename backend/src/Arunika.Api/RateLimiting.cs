using System.Globalization;
using System.Threading.RateLimiting;
using Arunika.Api.Contracts;
using Microsoft.AspNetCore.RateLimiting;

namespace Arunika.Api;

/// <summary>
/// Request throttling for the public API. Every read endpoint is anonymous, and
/// <c>GET /v1/briefing</c> can trigger on-demand AI generation, so an unthrottled
/// client can exhaust the AI provider's daily quota. Requests are partitioned by
/// client IP: a broad fixed window for the API at large, and a much tighter one for
/// the briefing endpoint via the <see cref="BriefingPolicy"/> policy.
/// </summary>
public static class RateLimiting
{
    /// <summary>Policy name for the endpoints that can start an AI generation.</summary>
    public const string BriefingPolicy = "briefing";

    private const int GlobalPermitsPerMinute = 100;
    private const int BriefingPermitsPerMinute = 10;

    public static IServiceCollection AddArunikaRateLimiting(this IServiceCollection services)
        => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                FixedWindowFor(context, GlobalPermitsPerMinute));

            options.AddPolicy(BriefingPolicy, context =>
                FixedWindowFor(context, BriefingPermitsPerMinute));

            // Same error envelope as the 400/404/500 responses, plus Retry-After so
            // clients back off instead of hammering.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ApiErrorEnvelope(new ApiErrorDetail(
                        "RATE_LIMITED", "Too many requests. Please retry shortly.")),
                    cancellationToken);
            };
        });

    private static RateLimitPartition<string> FixedWindowFor(HttpContext context, int permitLimit)
        => RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                // Reject immediately rather than parking requests; a queue here would
                // hold connections open on a single small instance.
                QueueLimit = 0,
            });

    /// <summary>
    /// Behind Render's proxy the socket address is the load balancer, so prefer the
    /// forwarded client address when present; fall back to the socket, then to a
    /// shared bucket so a request with neither still counts against something.
    /// </summary>
    private static string ClientKey(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
