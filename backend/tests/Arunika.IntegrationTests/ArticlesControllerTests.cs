using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Arunika.Api.Contracts;
using Arunika.IntegrationTests.Fakes;

namespace Arunika.IntegrationTests;

public class ArticlesControllerTests(ApiTestHost host) : IClassFixture<ApiTestHost>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetArticle_ReturnsDetailWithSectorsAndKeywords()
    {
        var response = await host.Client.GetAsync($"/v1/articles/{FakeArticleRepository.MarketsArticleId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDetailDto>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(FakeArticleRepository.MarketsArticleId, body!.Data.Id);
        Assert.Equal("Completed", body.Data.EnrichmentStatus);
        Assert.Equal(FakeArticleRepository.MarketsCategory, body.Data.Category);
        Assert.NotEmpty(body.Data.Sectors);
        Assert.NotEmpty(body.Data.Keywords);
        // The duplicate wire copy should show up as "also reported by" coverage.
        Assert.Single(body.Data.Duplicates);
        Assert.Equal(FakeArticleRepository.DuplicateArticleId, body.Data.Duplicates[0].Id);
    }

    [Fact]
    public async Task GetArticle_UnknownId_ReturnsNotFoundEnvelope()
    {
        var response = await host.Client.GetAsync($"/v1/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorEnvelope>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal("NOT_FOUND", body!.Error.Code);
    }
}
