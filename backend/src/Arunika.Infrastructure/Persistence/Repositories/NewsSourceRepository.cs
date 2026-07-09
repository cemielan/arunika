using Arunika.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence.Repositories;

public class NewsSourceRepository(ArunikaDbContext dbContext) : INewsSourceRepository
{
    public Task<Guid?> GetIdByNameAsync(string name, CancellationToken cancellationToken = default)
        => dbContext.NewsSources
            .Where(s => s.Name == name)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
