using Seminario.Domain.Entities;

namespace Seminario.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdWithDocumentsAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task<Role?> GetRoleByNameAsync(string name);
    Task AddAsync(User user);
    Task<(List<User> Items, int Total)> GetPagedAsync(string? search, string? role, string? status, int page, int pageSize, string? sortBy, string? sortDir);
    Task SaveChangesAsync();
}
