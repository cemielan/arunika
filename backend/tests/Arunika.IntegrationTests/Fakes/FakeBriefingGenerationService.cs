using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Arunika.Domain.Enums;

namespace Arunika.IntegrationTests.Fakes;

public class FakeBriefingGenerationService : IBriefingGenerationService
{
    public Task<BriefingGenerationResult> GenerateAsync(
        IReadOnlyList<Article> topStories,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new BriefingGenerationResult(
            "Fake executive summary.",
            Sentiment.Neutral,
            RiskLevel.Low));
}
