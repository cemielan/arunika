using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;

namespace Arunika.IntegrationTests.Fakes;

public class FakeArticleAnalysisRepository : IArticleAnalysisRepository
{
    public Task<Article?> GetArticleForEnrichmentAsync(Guid articleId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task SaveAnalysisAsync(Guid articleId, ArticleAnalysisResult result, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task MarkEnrichmentFailedAsync(Guid articleId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task<IReadOnlyList<Guid>> GetFailedArticleIdsAsync(int maxCount, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for Phase 6 API tests.");

    public Task SaveBriefingAsync(Briefing briefing, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<Briefing?> GetBriefingByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => Task.FromResult<Briefing?>(null);
}
