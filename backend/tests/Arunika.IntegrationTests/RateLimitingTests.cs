using System.Net;
using System.Net.Http.Json;
using Arunika.Api.Contracts;

namespace Arunika.IntegrationTests;

/// <summary>
/// Covers the throttling declared in <see cref="Arunika.Api.RateLimiting"/>. The
/// briefing endpoint is the one that matters most: a cache miss there can start an
/// AI generation, so an unthrottled caller could drain the provider's daily quota.
/// </summary>
public class RateLimitingTests(ApiTestHost host) : IClassFixture<ApiTestHost>
{
    private const int BriefingPermitsPerMinute = 10;

    [Fact]
    public async Task Briefing_BeyondItsPermitLimit_Returns429WithTheStandardErrorEnvelope()
    {
        for (var i = 0; i < BriefingPermitsPerMinute; i++)
        {
            var allowed = await host.Client.GetAsync("/v1/briefing");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var rejected = await host.Client.GetAsync("/v1/briefing");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        var envelope = await rejected.Content.ReadFromJsonAsync<ApiErrorEnvelope>();
        Assert.Equal("RATE_LIMITED", envelope?.Error.Code);

        // Clients need to know how long to wait; a fixed window always reports one.
        Assert.NotNull(rejected.Headers.RetryAfter);
    }
}
