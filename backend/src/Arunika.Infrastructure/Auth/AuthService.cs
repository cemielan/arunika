using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Arunika.Application.Abstractions;
using Arunika.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Arunika.Infrastructure.Auth;

public class AuthService(
    IUserRepository userRepository,
    IEmailService emailService,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
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

        await SendOtpAsync(user, cancellationToken);

        return new AuthResult(true,
            ErrorCode: "EMAIL_VERIFICATION_REQUIRED",
            ErrorMessage: "Account created. Please check your email for the verification code.");
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

        if (!user.EmailVerified)
        {
            return new AuthResult(false, ErrorCode: "EMAIL_NOT_VERIFIED", ErrorMessage: "Please verify your email before signing in.");
        }

        return GenerateAuthResult(user);
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuthResult(false, ErrorCode: "NOT_IMPLEMENTED", ErrorMessage: "Refresh token flow is not yet implemented."));
    }

    public async Task<AuthResult> VerifyOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return new AuthResult(false, ErrorCode: "INVALID_EMAIL", ErrorMessage: "No account found with this email.");
        }

        if (user.EmailVerified)
        {
            return new AuthResult(false, ErrorCode: "ALREADY_VERIFIED", ErrorMessage: "Email is already verified.");
        }

        if (user.OtpCode is null || user.OtpExpiresAt is null)
        {
            return new AuthResult(false, ErrorCode: "NO_OTP", ErrorMessage: "No verification code has been sent. Request a new one.");
        }

        if (DateTimeOffset.UtcNow > user.OtpExpiresAt.Value)
        {
            return new AuthResult(false, ErrorCode: "OTP_EXPIRED", ErrorMessage: "Verification code has expired. Request a new one.");
        }

        if (!string.Equals(user.OtpCode, otp, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult(false, ErrorCode: "INVALID_OTP", ErrorMessage: "Invalid verification code.");
        }

        user.EmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Email verified for user {Email}.", email);

        return GenerateAuthResult(user);
    }

    public async Task<AuthResult> ResendOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return new AuthResult(false, ErrorCode: "INVALID_EMAIL", ErrorMessage: "No account found with this email.");
        }

        if (user.EmailVerified)
        {
            return new AuthResult(false, ErrorCode: "ALREADY_VERIFIED", ErrorMessage: "Email is already verified.");
        }

        await SendOtpAsync(user, cancellationToken);

        return new AuthResult(true, ErrorMessage: "A new verification code has been sent to your email.");
    }

    public async Task<AuthResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return new AuthResult(true, ErrorMessage: "If the email is registered, you will receive a password reset link.");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.ResetToken = token;
        user.ResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        await userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendAsync(
                user.Email,
                "Reset your Arunika password",
                $"""
                <!DOCTYPE html>
                <html><body style="font-family: sans-serif; max-width: 480px; margin: 0 auto; padding: 24px;">
                <h1 style="font-size: 20px;">Reset your password</h1>
                <p style="color: #444;">Use the link below to reset your Arunika password. This link expires in 1 hour.</p>
                <div style="text-align: center; padding: 16px; margin: 16px 0;">
                    <a href="{GetResetUrl(user.Email, token)}"
                       style="display: inline-block; padding: 12px 24px; background: #0066cc; color: #fff; text-decoration: none; border-radius: 6px; font-size: 16px;">
                       Reset Password
                    </a>
                </div>
                <p style="color: #666; font-size: 13px;">If you did not request a password reset, you can safely ignore this email.</p>
                </body></html>
                """,
                cancellationToken);

            logger.LogInformation("Password reset email sent to {Email}.", user.Email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password reset email to {Email}.", user.Email);
        }

        return new AuthResult(true, ErrorMessage: "If the email is registered, you will receive a password reset link.");
    }

    public async Task<AuthResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return new AuthResult(false, ErrorCode: "INVALID_REQUEST", ErrorMessage: "Invalid or expired reset link.");
        }

        if (user.ResetToken is null || user.ResetTokenExpiresAt is null)
        {
            return new AuthResult(false, ErrorCode: "NO_RESET_REQUEST", ErrorMessage: "No password reset has been requested.");
        }

        if (DateTimeOffset.UtcNow > user.ResetTokenExpiresAt.Value)
        {
            return new AuthResult(false, ErrorCode: "RESET_TOKEN_EXPIRED", ErrorMessage: "Reset link has expired. Request a new one.");
        }

        if (!string.Equals(user.ResetToken, token, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult(false, ErrorCode: "INVALID_RESET_TOKEN", ErrorMessage: "Invalid reset link.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ResetToken = null;
        user.ResetTokenExpiresAt = null;
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Password reset for user {Email}.", email);

        return new AuthResult(true, ErrorMessage: "Your password has been reset successfully.");
    }

    private string GetResetUrl(string email, string token)
    {
        // Determine the frontend URL. In production this would come from config;
        // for development we use localhost:3000.
        return $"http://localhost:3000/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private async Task SendOtpAsync(User user, CancellationToken cancellationToken)
    {
        var otp = RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
        user.OtpCode = otp;
        user.OtpExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        await userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendAsync(
                user.Email,
                "Your Arunika verification code",
                $"""
                <!DOCTYPE html>
                <html><body style="font-family: sans-serif; max-width: 480px; margin: 0 auto; padding: 24px;">
                <h1 style="font-size: 20px;">Verify your email</h1>
                <p style="color: #444;">Use this code to verify your Arunika account:</p>
                <div style="font-size: 32px; letter-spacing: 8px; font-weight: bold; text-align: center; padding: 16px; background: #f5f5f5; border-radius: 8px; margin: 16px 0;">{otp}</div>
                <p style="color: #666; font-size: 13px;">This code expires in 15 minutes.</p>
                </body></html>
                """,
                cancellationToken);

            logger.LogInformation("OTP sent to {Email}.", user.Email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send OTP email to {Email}.", user.Email);
        }
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
