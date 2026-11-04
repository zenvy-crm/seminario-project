using Moq;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Users;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.Services;
using Seminario.UnitTests.Helpers;

namespace Seminario.UnitTests.Services;

[Trait("Category", "Unit")]
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly List<AuditEvent> _auditoria = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(a => a.Enqueue(It.IsAny<AuditEvent>())).Callback<AuditEvent>(_auditoria.Add);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => "bcrypt:" + p);
        _sut = new UserService(_users.Object, audit.Object, _hasher.Object);
    }

    private static CreateUserDto NuevoUsuario(string? rol = null) => new()
    {
        FirstName = "Luis",
        LastName = "Gomez",
        Username = "lgomez",
        Email = "lgomez@test.local",
        Password = "Segura#2026",
        BirthDate = "1998-03-01",
        Role = rol
    };

    [Theory(DisplayName = "UT-USR-01 Paginacion normaliza pagina y tamano fuera de rango")]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-3, 500, 1, 20)]
    [InlineData(2, 50, 2, 50)]
    public async Task Paginado_NormalizaParametros(int page, int size, int esperadaPage, int esperadoSize)
    {
        _users.Setup(u => u.GetPagedAsync(null, null, null, esperadaPage, esperadoSize, null, null))
            .ReturnsAsync((new List<User> { TestData.Usuario() }, 1));

        var resultado = await _sut.GetPagedAsync(new UserQueryDto { Page = page, PageSize = size });

        Assert.Equal(esperadaPage, resultado.Page);
        Assert.Equal(esperadoSize, resultado.PageSize);
        Assert.Single(resultado.Items);
        Assert.Equal(1, resultado.Total);
    }

    [Fact(DisplayName = "UT-USR-02 Consultar un usuario inexistente responde 404")]
    public async Task ObtenerPorId_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(404));
    }

    [Fact(DisplayName = "UT-USR-03 Crear usuario sin rol asigna USER, guarda el hash y audita")]
    public async Task Crear_SinRol_AsignaUser()
    {
        _users.Setup(u => u.GetRoleByNameAsync(RoleName.User)).ReturnsAsync(TestData.Rol(RoleName.User));
        User? guardado = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => { u.Id = 7; guardado = u; });

        var dto = await _sut.CreateAsync(NuevoUsuario());

        Assert.Equal(RoleName.User, dto.Role);
        Assert.Equal("bcrypt:Segura#2026", guardado!.PasswordHash);
        Assert.Equal("1998-03-01", dto.BirthDate);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.UserCreated);
    }

    [Fact(DisplayName = "UT-USR-04 Crear usuario normaliza el rol a mayusculas")]
    public async Task Crear_RolEnMinusculas_SeNormaliza()
    {
        _users.Setup(u => u.GetRoleByNameAsync(RoleName.Admin)).ReturnsAsync(TestData.Rol(RoleName.Admin, 1));

        var dto = await _sut.CreateAsync(NuevoUsuario("admin"));

        Assert.Equal(RoleName.Admin, dto.Role);
    }

    [Fact(DisplayName = "UT-USR-05 Crear usuario con rol inexistente responde 422")]
    public async Task Crear_RolInvalido_Lanza422()
    {
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() => _sut.CreateAsync(NuevoUsuario("SUPERUSER")));
        Assert.Equal(422, ex.StatusCode);
        Assert.Contains("role", ex.Errors);
    }

    [Fact(DisplayName = "UT-USR-06 Crear usuario con username o correo duplicado responde 409")]
    public async Task Crear_Duplicado_Lanza409()
    {
        _users.Setup(u => u.UsernameExistsAsync("lgomez", null)).ReturnsAsync(true);
        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(NuevoUsuario()));

        _users.Setup(u => u.UsernameExistsAsync("lgomez", null)).ReturnsAsync(false);
        _users.Setup(u => u.EmailExistsAsync("lgomez@test.local", null)).ReturnsAsync(true);
        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(NuevoUsuario()));
    }

    [Fact(DisplayName = "UT-USR-07 Actualizar solo registra en auditoria los campos que cambiaron")]
    public async Task Actualizar_CamposCambiados_AuditaAntesYDespues()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(user);

        var dto = await _sut.UpdateAsync(10, new UpdateUserDto
        {
            FirstName = "Juan Carlos",
            LastName = user.LastName,
            Email = "nuevo@test.local",
            Phone = "4444-1111",
            Identification = "999",
            BirthDate = "1990-12-31"
        });

        Assert.Equal("Juan Carlos", dto.FirstName);
        Assert.Equal("nuevo@test.local", dto.Email);
        Assert.Equal("1990-12-31", dto.BirthDate);
        var evento = Assert.Single(_auditoria, a => a.EventType == AuditEventTypes.UserUpdated);
        var antes = Assert.IsType<Dictionary<string, object?>>(evento.Before);
        Assert.Equal(5, antes.Count);
        Assert.False(antes.ContainsKey("lastName"));
        _users.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact(DisplayName = "UT-USR-08 Actualizar con un correo de otro usuario responde 409")]
    public async Task Actualizar_CorreoDeOtro_Lanza409()
    {
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(TestData.Usuario());
        _users.Setup(u => u.EmailExistsAsync("ocupado@test.local", 10)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateAsync(10, new UpdateUserDto { Email = "ocupado@test.local" }));
    }

    [Fact(DisplayName = "UT-USR-09 Cambiar rol y estado audita USER_ROLE_CHANGED y USER_STATUS_CHANGED")]
    public async Task Actualizar_RolYEstado_Audita()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(user);
        _users.Setup(u => u.GetRoleByNameAsync(RoleName.Intermedio)).ReturnsAsync(TestData.Rol(RoleName.Intermedio, 2));

        var dto = await _sut.UpdateAsync(10, new UpdateUserDto { Role = "intermedio", Status = "inactive" });

        Assert.Equal(RoleName.Intermedio, dto.Role);
        Assert.Equal("INACTIVE", dto.Status);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.UserRoleChanged);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.UserStatusChanged);
        Assert.DoesNotContain(_auditoria, a => a.EventType == AuditEventTypes.UserUpdated);
    }

    [Fact(DisplayName = "UT-USR-10 Cambiar a un rol inexistente responde 422")]
    public async Task Actualizar_RolInexistente_Lanza422()
    {
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(TestData.Usuario());

        await Assert.ThrowsAsync<ValidationApiException>(() => _sut.UpdateAsync(10, new UpdateUserDto { Role = "ROOT" }));
    }

    [Fact(DisplayName = "UT-USR-11 Estado invalido se ignora sin romper la actualizacion")]
    public async Task Actualizar_EstadoInvalido_SeIgnora()
    {
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(TestData.Usuario());

        var dto = await _sut.UpdateAsync(10, new UpdateUserDto { Status = "BORRADO" });

        Assert.Equal("ACTIVE", dto.Status);
        Assert.Empty(_auditoria);
    }

    [Fact(DisplayName = "UT-USR-12 Actualizar un usuario inexistente responde 404")]
    public async Task Actualizar_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, new UpdateUserDto()));
    }

    [Fact(DisplayName = "UT-USR-13 [Regresion] Desbloqueo manual del admin limpia intentos y bloqueo")]
    [Trait("Category", "Regression")]
    public async Task Desbloquear_CuentaBloqueada_Limpia()
    {
        var user = TestData.Usuario();
        user.FailedLoginAttempts = 3;
        user.LockedUntil = DateTime.UtcNow.AddMinutes(4);
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(user);

        var dto = await _sut.UnlockAsync(10);

        Assert.False(dto.IsLocked);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.AccountUnlocked);
    }

    [Fact(DisplayName = "UT-USR-14 Desbloquear una cuenta no bloqueada no guarda ni audita")]
    public async Task Desbloquear_SinBloqueo_NoHaceNada()
    {
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(TestData.Usuario());

        await _sut.UnlockAsync(10);

        _users.Verify(u => u.SaveChangesAsync(), Times.Never);
        Assert.Empty(_auditoria);
    }

    [Fact(DisplayName = "UT-USR-15 Desbloquear un usuario inexistente responde 404")]
    public async Task Desbloquear_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UnlockAsync(5));
    }
}
