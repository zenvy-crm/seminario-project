using Seminario.Application.DTOs.Auth;

namespace Seminario.Application.Interfaces;

public interface IAuthService
{
    Task<(AuthResponseDto Response, int UserId)> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResult> LoginAsync(LoginRequestDto dto, string? ipAddress);
    Task<AuthResult> VerifyMfaAsync(MfaVerifyRequestDto dto, string? ipAddress);
    Task<AuthResult> RefreshAsync(string refreshToken, string? ipAddress);
    Task LogoutAsync(string? refreshToken);
}
