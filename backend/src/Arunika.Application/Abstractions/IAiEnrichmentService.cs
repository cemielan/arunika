using Arunika.Domain.Entities;
using Arunika.Domain.Enums;

namespace Arunika.Application.Abstractions;

public record SectorImpactResult(string Sector, ImpactDirection Direction, int Magnitude);

/// <summary>
/// The structured output of one AI enrichment call, matching the schema in
/// design doc §5 (summary, category, sentiment, impact score, sector impact,
/// keywords) — combined into a single call rather than one per field.
/// </summary>
public record ArticleAnalysisResult(
    string Summary,
    string Category,
    Sentiment Sentiment,
    float SentimentConfidence,
    int ImpactScore,
    string? ImpactRationale,
    IReadOnlyList<SectorImpactResult> Sectors,
    IReadOnlyList<string> Keywords
);

/// <summary>
/// Produces structured, decision-ready enrichment for one article. Implemented
/// in Infrastructure against a specific AI provider (Gemini, to start).
/// </summary>
public interface IAiEnrichmentService
{
    Task<ArticleAnalysisResult> AnalyzeAsync(Article article, CancellationToken cancellationToken = default);
}
