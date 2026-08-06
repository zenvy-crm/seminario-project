namespace Seminario.Application.DTOs.Auth;

public record AuthResult(AuthResponseDto Response, string? RefreshToken);
