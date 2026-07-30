using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.AI;

public class CompositeAiEnrichmentService(
    IEnumerable<IAiEnrichmentService> services,
    ILogger<CompositeAiEnrichmentService> logger) : IAiEnrichmentService
{
    public async Task<ArticleAnalysisResult> AnalyzeAsync(Article article, CancellationToken cancellationToken = default)
    {
        var exceptions = new List<Exception>();

        foreach (var service in services)
        {
            try
            {
                var result = await service.AnalyzeAsync(article, cancellationToken);
                logger.LogInformation(
                    "CompositeAiEnrichmentService: enrichment succeeded for article {ArticleId} using {Service}.",
                    article.Id, service.GetType().Name);
                return result;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                logger.LogWarning(ex,
                    "CompositeAiEnrichmentService: {Service} failed for article {ArticleId}; trying next provider.",
                    service.GetType().Name, article.Id);
            }
        }

        throw new AggregateException(
            $"All AI enrichment providers failed for article {article.Id}.",
            exceptions);
    }
}
