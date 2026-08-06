using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seminario.Application.DTOs.Auth;
using Seminario.Application.Interfaces;

namespace Seminario.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    private readonly IAuthService _authService;
    private readonly IDocumentService _documentService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IDocumentService documentService, IWebHostEnvironment env)
    {
        _authService = authService;
        _documentService = documentService;
        _env = env;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromForm] RegisterRequestDto dto, IFormFile? document)
    {
        var (response, userId) = await _authService.RegisterAsync(dto);

        if (document is not null)
        {
            await _documentService.UploadAsync(userId, document);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString());
        SetRefreshCookieIfPresent(result.RefreshToken);
        return Ok(result.Response);
    }

    [AllowAnonymous]
    [HttpPost("mfa/verify")]
    public async Task<ActionResult<AuthResponseDto>> VerifyMfa([FromBody] MfaVerifyRequestDto dto)
    {
        var result = await _authService.VerifyMfaAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString());
        SetRefreshCookieIfPresent(result.RefreshToken);
        return Ok(result.Response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;
        var result = await _authService.RefreshAsync(refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());
        SetRefreshCookieIfPresent(result.RefreshToken);
        return Ok(result.Response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        await _authService.LogoutAsync(refreshToken);
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
        return Ok(new { success = true });
    }

    private void SetRefreshCookieIfPresent(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
