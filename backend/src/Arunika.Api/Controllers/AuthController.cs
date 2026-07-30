using Arunika.Api.Contracts;
using Arunika.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Arunika.Api.Controllers;

[ApiController]
[Route("v1/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request.Email, request.Password, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "EMAIL_EXISTS" => Conflict(new ApiErrorEnvelope(new ApiErrorDetail("EMAIL_EXISTS", result.ErrorMessage!))),
                _ => BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(result.ErrorCode!, result.ErrorMessage!))),
            };
        }

        return Ok(new ApiResponse<AuthResponse>(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.User!)));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new ApiErrorEnvelope(new ApiErrorDetail(result.ErrorCode!, result.ErrorMessage!)));
        }

        return Ok(new ApiResponse<AuthResponse>(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.User!)));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new ApiErrorEnvelope(new ApiErrorDetail(result.ErrorCode!, result.ErrorMessage!)));
        }

        return Ok(new ApiResponse<AuthResponse>(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.User!)));
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(result.ErrorCode!, result.ErrorMessage!)));
        }

        return Ok(new ApiResponse<AuthResponse>(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.User!)));
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok(new ApiResponse<object>(new { }));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(result.ErrorCode!, result.ErrorMessage!)));
        }

        return Ok(new ApiResponse<object>(new { }));
    }

    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResendOtpAsync(request.Email, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ApiErrorEnvelope(new ApiErrorDetail(result.ErrorCode!, result.ErrorMessage!)));
        }

        return Ok(new ApiResponse<object>(new { }));
    }
}
