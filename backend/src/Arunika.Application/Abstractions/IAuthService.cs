namespace Arunika.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthResult> VerifyOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
    Task<AuthResult> ResendOtpAsync(string email, CancellationToken cancellationToken = default);
}

public sealed record AuthResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    UserDto? User = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record VerifyOtpRequest(string Email, string Otp);

public sealed record ResendOtpRequest(string Email);

public sealed record AuthResponse(string AccessToken, string RefreshToken, UserDto User);

public sealed record UserDto(Guid Id, string Email, string Role);
