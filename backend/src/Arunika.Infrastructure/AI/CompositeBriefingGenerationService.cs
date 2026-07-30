using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Arunika.Infrastructure.AI;

public class CompositeBriefingGenerationService(
    IEnumerable<IBriefingGenerationService> services,
    ILogger<CompositeBriefingGenerationService> logger) : IBriefingGenerationService
{
    public async Task<BriefingGenerationResult> GenerateAsync(IReadOnlyList<Article> topStories, CancellationToken cancellationToken = default)
    {
        var exceptions = new List<Exception>();

        foreach (var service in services)
        {
            try
            {
                var result = await service.GenerateAsync(topStories, cancellationToken);
                logger.LogInformation(
                    "CompositeBriefingGenerationService: briefing generation succeeded using {Service}.",
                    service.GetType().Name);
                return result;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                logger.LogWarning(ex,
                    "CompositeBriefingGenerationService: {Service} failed; trying next provider.",
                    service.GetType().Name);
            }
        }

        throw new AggregateException(
            "All briefing generation providers failed.",
            exceptions);
    }
}
