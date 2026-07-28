namespace Arunika.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
