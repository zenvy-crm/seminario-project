using Microsoft.EntityFrameworkCore;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByIdWithDocumentsAsync(int id) =>
        _db.Users.Include(u => u.Role).Include(u => u.Documents).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string username) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);

    public Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null) =>
        _db.Users.AnyAsync(u => u.Username == username && (excludeUserId == null || u.Id != excludeUserId));

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null) =>
        _db.Users.AnyAsync(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId));

    public Task<Role?> GetRoleByNameAsync(string name) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task<(List<User> Items, int Total)> GetPagedAsync(
        string? search, string? role, string? status, int page, int pageSize, string? sortBy, string? sortDir)
    {
        var query = _db.Users.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role.Name == role);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Enums.UserStatus>(status, true, out var statusEnum))
        {
            query = query.Where(u => u.Status == statusEnum);
        }

        var total = await query.CountAsync();

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLower() switch
        {
            "username" => descending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            _ => descending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
