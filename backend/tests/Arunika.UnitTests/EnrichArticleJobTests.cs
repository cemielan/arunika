using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Arunika.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arunika.UnitTests;

/// <summary>
/// Guards the rule that scoring rubric changes apply to new enrichments only.
/// SaveAnalysisAsync is an idempotent upsert, so without the Completed check a
/// replayed job would overwrite a finished analysis with a score from whatever
/// rubric is current — rescoring an article nobody asked to revisit.
/// </summary>
public class EnrichArticleJobTests
{
    [Fact]
    public async Task RunAsync_LeavesAnAlreadyEnrichedArticleAlone()
    {
        var repository = new StubRepository(ArticleWith(EnrichmentStatus.Completed));
        var enrichment = new StubEnrichment();
        var job = new EnrichArticleJob(enrichment, repository, NullLogger<EnrichArticleJob>.Instance);

        await job.RunAsync(Guid.NewGuid());

        Assert.False(enrichment.WasCalled);
        Assert.False(repository.AnalysisWasSaved);
    }

    [Theory]
    [InlineData(EnrichmentStatus.Pending)]
    [InlineData(EnrichmentStatus.Failed)]
    public async Task RunAsync_EnrichesArticlesThatHaveNoAnalysisYet(EnrichmentStatus status)
    {
        var repository = new StubRepository(ArticleWith(status));
        var enrichment = new StubEnrichment();
        var job = new EnrichArticleJob(enrichment, repository, NullLogger<EnrichArticleJob>.Instance);

        await job.RunAsync(Guid.NewGuid());

        Assert.True(enrichment.WasCalled);
        Assert.True(repository.AnalysisWasSaved);
    }

    private static Article ArticleWith(EnrichmentStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Placeholder headline",
        Url = "https://example.test/story",
        RawContent = "Placeholder body.",
        DedupeHash = "hash",
        EnrichmentStatus = status
    };

    private sealed class StubEnrichment : IAiEnrichmentService
    {
        public bool WasCalled { get; private set; }

        public Task<ArticleAnalysisResult> AnalyzeAsync(Article article, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new ArticleAnalysisResult(
                "Summary.", "Markets", Sentiment.Neutral, 0.5f, 61, "Rationale.", [], [], "stub"));
        }
    }

    private sealed class StubRepository(Article article) : IArticleAnalysisRepository
    {
        public bool AnalysisWasSaved { get; private set; }

        public Task<Article?> GetArticleForEnrichmentAsync(Guid articleId, CancellationToken cancellationToken = default)
            => Task.FromResult<Article?>(article);

        public Task SaveAnalysisAsync(Guid articleId, ArticleAnalysisResult result, CancellationToken cancellationToken = default)
        {
            AnalysisWasSaved = true;
            return Task.CompletedTask;
        }

        public Task MarkEnrichmentFailedAsync(Guid articleId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Guid>> GetFailedArticleIdsAsync(int maxCount, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<int> GetEnrichmentRetryCountAsync(Guid articleId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task SaveBriefingAsync(Briefing briefing, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Briefing?> GetBriefingByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult<Briefing?>(null);

        public Task<Briefing?> GetLatestBriefingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Briefing?>(null);
    }
}
