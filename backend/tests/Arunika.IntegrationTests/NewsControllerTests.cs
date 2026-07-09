using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Api.Contracts;
using Arunika.IntegrationTests.Fakes;

namespace Arunika.IntegrationTests;

public class NewsControllerTests(ApiTestHost host) : IClassFixture<ApiTestHost>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetNews_ReturnsEnvelopeWithArticlesAndPageMeta()
    {
        var response = await host.Client.GetAsync("/v1/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<NewsListItemDto>>>(JsonOptions);

        Assert.NotNull(body);
        // Only non-duplicate articles should appear (3 of the 4 seeded articles).
        Assert.Equal(3, body!.Data.Count);
        Assert.DoesNotContain(body.Data, item => item.Id == FakeArticleRepository.DuplicateArticleId);
    }

    [Fact]
    public async Task GetNews_FiltersByCategory()
    {
        var response = await host.Client.GetAsync($"/v1/news?category={FakeArticleRepository.MarketsCategory}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<NewsListItemDto>>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Single(body!.Data);
        Assert.All(body.Data, item => Assert.Equal(FakeArticleRepository.MarketsCategory, item.Category));
    }

    [Fact]
    public async Task GetNews_RespectsPageSize()
    {
        var response = await host.Client.GetAsync("/v1/news?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<NewsListItemDto>>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Single(body!.Data);
    }
}
