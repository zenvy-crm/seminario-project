using Microsoft.EntityFrameworkCore;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Repositories;

public class RefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _db;

    public RefreshTokenStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash) =>
        _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow);

    public Task RevokeAsync(RefreshToken token)
    {
        token.RevokedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
