using Seminario.Domain.Entities;

namespace Seminario.Application.Interfaces;

public interface IRefreshTokenStore
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash);
    Task RevokeAsync(RefreshToken token);
    Task SaveChangesAsync();
}
