using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence.Repositories;

public class SectorStatsRepository(ArunikaDbContext dbContext) : ISectorStatsRepository
{
    public async Task ReplaceRangeAsync(DateOnly from, DateOnly to, IReadOnlyList<SectorDailyStat> stats, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.SectorDailyStats
            .Where(s => s.Date >= from && s.Date <= to)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.SectorDailyStats.AddRange(stats);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SectorDailyStat>> GetSinceAsync(DateOnly from, CancellationToken cancellationToken = default)
        => await dbContext.SectorDailyStats
            .Where(s => s.Date >= from)
            .Include(s => s.Sector)
            .OrderBy(s => s.Date)
            .ToListAsync(cancellationToken);
}
