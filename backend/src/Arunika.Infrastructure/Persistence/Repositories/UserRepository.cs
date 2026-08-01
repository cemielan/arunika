using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence.Repositories;

public class UserRepository(ArunikaDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await dbContext.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetDigestSubscribersAsync(CancellationToken cancellationToken = default)
        => await dbContext.Users
            .Where(u => u.EmailVerified && u.DigestEnabled)
            .ToListAsync(cancellationToken);

    public async Task UpsertAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken)
            ?? await dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email, cancellationToken);

        if (existing is null)
        {
            await dbContext.Users.AddAsync(user, cancellationToken);
            return;
        }

        existing.Email = user.Email;
        existing.EmailVerified = user.EmailVerified;
        existing.Role = user.Role;
    }
}
