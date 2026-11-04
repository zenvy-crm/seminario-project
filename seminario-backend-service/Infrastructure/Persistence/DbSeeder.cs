using Microsoft.EntityFrameworkCore;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;

namespace Seminario.Infrastructure.Persistence;

public static class DbSeeder
{
    // Roles de la Fase 2: USER = Estandar (1), INTERMEDIO = Intermedio (2), ADMIN = Administrador (3).
    private static readonly (string Name, string Description)[] RolesBase =
    {
        (RoleName.Admin, "Administrador del sistema"),
        (RoleName.Intermedio, "Usuario intermedio: puede anular lineas del detalle de una orden"),
        (RoleName.User, "Usuario estandar")
    };

    // Timeout de las migraciones: agregar rowversion o crear un indice sobre TB_Ordenes_Compra
    // recorre toda la tabla, y con millones de filas del stress test 30 s (el default) no alcanzan.
    private static readonly TimeSpan TimeoutMigraciones = TimeSpan.FromMinutes(10);

    // Administrador inicial: solo se crea si no existe NINGUN usuario con rol ADMIN (regla original),
    // asi un admin renombrado o con otro email no vuelve a aparecer con la contrasena publicada.
    private static readonly (string Username, string Password, string Role, string FirstName, string LastName, string Email) AdminInicial =
        ("admin", "Admin#2026!", RoleName.Admin, "Admin", "Sistema", "admin@seminario.local");

    // Usuarios de demostracion de los niveles 1 y 2. Solo se siembran en Development o con
    // Seed:DemoUsers=true (variable Seed__DemoUsers en el .env), nunca por defecto en produccion.
    private static readonly (string Username, string Password, string Role, string FirstName, string LastName, string Email)[] UsuariosDemo =
    {
        ("intermedio", "Intermedio#2026!", RoleName.Intermedio, "Usuario", "Intermedio", "intermedio@seminario.local"),
        ("estandar", "Estandar#2026!", RoleName.User, "Usuario", "Estandar", "estandar@seminario.local")
    };

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, bool sembrarUsuariosDemo)
    {
        var timeoutAnterior = db.Database.GetCommandTimeout();
        db.Database.SetCommandTimeout(TimeoutMigraciones);
        try
        {
            await db.Database.MigrateAsync();
        }
        finally
        {
            db.Database.SetCommandTimeout(timeoutAnterior);
        }

        // Siembra por nombre: agrega los roles que falten sin tocar los existentes.
        var rolesExistentes = await db.Roles.Select(r => r.Name).ToListAsync();
        var rolesNuevos = RolesBase
            .Where(r => !rolesExistentes.Contains(r.Name))
            .Select(r => new Role { Name = r.Name, Description = r.Description })
            .ToList();

        if (rolesNuevos.Count > 0)
        {
            db.Roles.AddRange(rolesNuevos);
            await db.SaveChangesAsync();
        }

        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);

        var adminRoleId = roles[RoleName.Admin];
        if (!await db.Users.AnyAsync(x => x.RoleId == adminRoleId))
        {
            await AgregarSiNoExisteAsync(db, passwordHasher, roles, AdminInicial);
        }

        if (sembrarUsuariosDemo)
        {
            foreach (var u in UsuariosDemo)
            {
                await AgregarSiNoExisteAsync(db, passwordHasher, roles, u);
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Agrega el usuario solo si no existe otro con el mismo username o email.</summary>
    private static async Task AgregarSiNoExisteAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IReadOnlyDictionary<string, int> roles,
        (string Username, string Password, string Role, string FirstName, string LastName, string Email) u)
    {
        var existe = await db.Users.AnyAsync(x => x.Username == u.Username || x.Email == u.Email);
        if (existe)
        {
            return;
        }

        var user = new User
        {
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username,
            Email = u.Email,
            RoleId = roles[u.Role],
            Status = UserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.Hash(u.Password);

        db.Users.Add(user);
    }
}
