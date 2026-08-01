using Arunika.Domain.Entities;

namespace Arunika.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetDigestSubscribersAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(User user, CancellationToken cancellationToken = default);
}
