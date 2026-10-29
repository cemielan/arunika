using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;

namespace Arunika.IntegrationTests.Fakes;

public class FakeSectorStatsRepository : ISectorStatsRepository
{
    public Task ReplaceRangeAsync(DateOnly from, DateOnly to, IReadOnlyList<SectorDailyStat> stats, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed for API tests.");

    public Task<IReadOnlyList<SectorDailyStat>> GetSinceAsync(DateOnly from, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SectorDailyStat>>([]);
}
