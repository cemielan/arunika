using System.Net;
using Arunika.Api.Contracts;

namespace Arunika.Api.Middleware;

/// <summary>
/// Catches any exception that escapes controller action execution and returns
/// the standard error envelope (design doc §7) instead of the framework's
/// default HTML/ProblemDetails error page.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var envelope = new ApiErrorEnvelope(new ApiErrorDetail("INTERNAL_ERROR", "An unexpected error occurred."));
            await context.Response.WriteAsJsonAsync(envelope);
        }
    }
}
