using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Api.Contracts;
using Arunika.IntegrationTests.Fakes;

namespace Arunika.IntegrationTests;

public class BriefingControllerTests(ApiTestHost host) : IClassFixture<ApiTestHost>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetBriefing_ReturnsTopStoriesOrderedByImpactScore()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = await host.Client.GetAsync($"/v1/briefing?date={today:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BriefingResponseDto>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(today, body!.Data.Date);
        // The unenriched article has no impact score and must be excluded.
        Assert.Equal(2, body.Data.TopStories.Count);
        Assert.Equal(FakeArticleRepository.MarketsArticleId, body.Data.TopStories[0].ArticleId);
        Assert.True(body.Data.TopStories[0].ImpactScore >= body.Data.TopStories[1].ImpactScore);
    }

    [Fact]
    public async Task GetBriefing_PastDateWithNoArticles_ReturnsEmptyTopStories()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1);

        var response = await host.Client.GetAsync($"/v1/briefing?date={pastDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BriefingResponseDto>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Empty(body!.Data.TopStories);
    }
}
