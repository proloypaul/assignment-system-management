using Asp.Versioning;
using AssignmentSystem.Application.Features.Auth.Commands.Login;
using AssignmentSystem.Application.Features.Auth.Commands.RefreshToken;
using AssignmentSystem.Application.Features.Auth.Commands.SignOut;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Authenticate user and set HttpOnly JWT cookies.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));

        SetTokenCookies(result.AccessToken, result.RefreshToken);

        return Ok(new
        {
            success = true,
            statusCode = 200,
            message = "Login successful.",
            data = new { id = result.UserId, name = result.Name, email = result.Email, role = result.Role }
        });
    }

    /// <summary>Register a new student or teacher and set HttpOnly JWT cookies.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new AssignmentSystem.Application.Features.Auth.Commands.Register.RegisterCommand(request.Name, request.Email, request.Password, request.Role));

        SetTokenCookies(result.AccessToken, result.RefreshToken);

        return Ok(new
        {
            success = true,
            statusCode = 200,
            message = "Registration successful.",
            data = new { id = result.UserId, name = result.Name, email = result.Email, role = result.Role }
        });
    }

    /// <summary>Use a valid refresh token cookie to obtain a new access token.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { message = "Refresh token not found." });

        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));

        SetTokenCookies(result.AccessToken, result.RefreshToken);

        return Ok(new { message = "Token refreshed." });
    }

    /// <summary>Sign out and clear authentication cookies.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _mediator.Send(new SignOutCommand());
        ClearTokenCookies();
        return Ok(new { message = "Signed out successfully." });
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var isSecure = bool.Parse(HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Cookie__Secure"] ?? "false");

        var accessTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60)
        };

        var refreshTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("accessToken", accessToken, accessTokenOptions);
        Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
    }

    private void ClearTokenCookies()
    {
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password, string Role);
