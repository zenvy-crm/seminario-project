using Seminario.Domain.Entities;
using Seminario.Domain.Enums;

namespace Seminario.UnitTests.Helpers;

/// <summary>Fabrica de entidades de prueba con valores validos por defecto.</summary>
internal static class TestData
{
    public static Role Rol(string nombre = RoleName.User, int id = 3) => new() { Id = id, Name = nombre };

    public static User Usuario(
        int id = 10,
        string username = "jperez",
        string rol = RoleName.User,
        UserStatus status = UserStatus.ACTIVE,
        string passwordHash = "$2a$12$hash")
    {
        var role = Rol(rol);
        return new User
        {
            Id = id,
            FirstName = "Juan",
            LastName = "Perez",
            Username = username,
            Email = $"{username}@test.local",
            Phone = "5555-0000",
            IdentificationNumber = "1234567890101",
            BirthDate = new DateOnly(1999, 1, 15),
            PasswordHash = passwordHash,
            RoleId = role.Id,
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
    }
}
