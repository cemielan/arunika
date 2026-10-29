using Arunika.Domain.Entities;
using Arunika.Domain.Enums;

namespace Arunika.Application.Abstractions;

public record BriefingGenerationResult(
    string ExecutiveSummary,
    Sentiment OverallSentiment,
    RiskLevel RiskLevel
);

public interface IBriefingGenerationService
{
    Task<BriefingGenerationResult> GenerateAsync(IReadOnlyList<Article> topStories, CancellationToken cancellationToken = default);
}
