using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Arunika.Infrastructure.Auth;

public class AuthService(
    IUserRepository userRepository,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return new AuthResult(false, ErrorCode: "EMAIL_EXISTS", ErrorMessage: "An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return GenerateAuthResult(user);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return new AuthResult(false, ErrorCode: "INVALID_CREDENTIALS", ErrorMessage: "Invalid email or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new AuthResult(false, ErrorCode: "INVALID_CREDENTIALS", ErrorMessage: "Invalid email or password.");
        }

        return GenerateAuthResult(user);
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // V1: refresh tokens are not yet persisted. This is a placeholder
        // that will be wired to a refresh-token store in a follow-up.
        return Task.FromResult(new AuthResult(false, ErrorCode: "NOT_IMPLEMENTED", ErrorMessage: "Refresh token flow is not yet implemented."));
    }

    private AuthResult GenerateAuthResult(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        return new AuthResult(true, accessToken, refreshToken, User: new UserDto(user.Id, user.Email, user.Role));
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
