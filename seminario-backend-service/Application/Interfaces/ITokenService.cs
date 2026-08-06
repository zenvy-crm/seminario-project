using Seminario.Domain.Entities;

namespace Seminario.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string rawToken);
    int RefreshTokenExpirationDays { get; }
}
