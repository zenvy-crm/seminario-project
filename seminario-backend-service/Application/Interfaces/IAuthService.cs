using Seminario.Application.DTOs.Auth;

namespace Seminario.Application.Interfaces;

public interface IAuthService
{
    Task<(AuthResponseDto Response, int UserId)> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResult> LoginAsync(LoginRequestDto dto);
    Task<AuthResult> VerifyMfaAsync(MfaVerifyRequestDto dto);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string? refreshToken);
}
