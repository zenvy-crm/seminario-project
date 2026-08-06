using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;

namespace Seminario.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { Name = RoleName.Admin, Description = "Administrador del sistema" },
                new Role { Name = RoleName.User, Description = "Usuario estandar" }
            );
            await db.SaveChangesAsync();
        }

        if (!db.Users.Any())
        {
            var adminRole = db.Roles.First(r => r.Name == RoleName.Admin);
            var hasher = new PasswordHasher<User>();

            var admin = new User
            {
                FirstName = "Admin",
                LastName = "Sistema",
                Username = "admin",
                Email = "admin@seminario.local",
                RoleId = adminRole.Id,
                Status = UserStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin#2026!");

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
