namespace Arunika.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool EmailVerified { get; set; }
    public string? OtpCode { get; set; }
    public DateTimeOffset? OtpExpiresAt { get; set; }
    public bool DigestEnabled { get; set; } = true;

    public string? ResetToken { get; set; }
    public DateTimeOffset? ResetTokenExpiresAt { get; set; }
}
