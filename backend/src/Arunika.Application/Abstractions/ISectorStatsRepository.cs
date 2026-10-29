using Arunika.Domain.Entities;

namespace Arunika.Application.Abstractions;

/// <summary>
/// Daily per-sector snapshots that back the sector trend. Implemented in
/// Infrastructure (EF Core).
/// </summary>
public interface ISectorStatsRepository
{
    /// <summary>
    /// Replaces every stored row dated <paramref name="from"/> through
    /// <paramref name="to"/> (inclusive) with <paramref name="stats"/>, so a
    /// day can be recounted while its articles still exist.
    /// </summary>
    Task ReplaceRangeAsync(DateOnly from, DateOnly to, IReadOnlyList<SectorDailyStat> stats, CancellationToken cancellationToken = default);

    /// <summary>Rows dated on or after <paramref name="from"/>, oldest first, with their sector loaded.</summary>
    Task<IReadOnlyList<SectorDailyStat>> GetSinceAsync(DateOnly from, CancellationToken cancellationToken = default);
}
