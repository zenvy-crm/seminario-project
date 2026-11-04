using Moq;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Profile;
using Seminario.Application.Interfaces;
using Seminario.Infrastructure.Services;
using Seminario.UnitTests.Helpers;

namespace Seminario.UnitTests.Services;

[Trait("Category", "Unit")]
public class ProfileServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly List<AuditEvent> _auditoria = new();
    private readonly ProfileService _sut;

    public ProfileServiceTests()
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(a => a.Enqueue(It.IsAny<AuditEvent>())).Callback<AuditEvent>(_auditoria.Add);
        _sut = new ProfileService(_users.Object, audit.Object, _hasher.Object);
    }

    [Fact(DisplayName = "UT-PRF-01 Perfil de un usuario inexistente responde 404")]
    public async Task Obtener_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync(1));
    }

    [Fact(DisplayName = "UT-PRF-02 Perfil existente se devuelve como DTO")]
    public async Task Obtener_Existente_DevuelveDto()
    {
        _users.Setup(u => u.GetByIdWithDocumentsAsync(10)).ReturnsAsync(TestData.Usuario());

        var dto = await _sut.GetAsync(10);

        Assert.Equal("jperez", dto.Username);
        Assert.NotNull(dto.Documents);
    }

    [Fact(DisplayName = "UT-PRF-03 Actualizar perfil sin cambios no guarda ni audita")]
    public async Task Actualizar_SinCambios_NoGuarda()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(user);

        await _sut.UpdateAsync(10, new UpdateProfileDto { FirstName = user.FirstName });

        _users.Verify(u => u.SaveChangesAsync(), Times.Never);
        Assert.Empty(_auditoria);
    }

    [Fact(DisplayName = "UT-PRF-04 Actualizar perfil guarda y audita los campos cambiados")]
    public async Task Actualizar_ConCambios_GuardaYAudita()
    {
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(TestData.Usuario());

        var dto = await _sut.UpdateAsync(10, new UpdateProfileDto
        {
            FirstName = "Pedro",
            LastName = "Alvarado",
            Email = "pedro@test.local",
            Phone = "1111-2222",
            Identification = "777"
        });

        Assert.Equal("Pedro", dto.FirstName);
        Assert.Equal("777", dto.Identification);
        _users.Verify(u => u.SaveChangesAsync(), Times.Once);
        var evento = Assert.Single(_auditoria);
        Assert.Equal(AuditEventTypes.UserUpdated, evento.EventType);
    }

    [Fact(DisplayName = "UT-PRF-05 Actualizar perfil con correo de otro usuario responde 409")]
    public async Task Actualizar_CorreoOcupado_Lanza409()
    {
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(TestData.Usuario());
        _users.Setup(u => u.EmailExistsAsync("ocupado@test.local", 10)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateAsync(10, new UpdateProfileDto { Email = "ocupado@test.local" }));
    }

    [Fact(DisplayName = "UT-PRF-06 Actualizar perfil de usuario inexistente responde 404")]
    public async Task Actualizar_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(10, new UpdateProfileDto()));
    }

    [Fact(DisplayName = "UT-PRF-07 Cambio de contrasena con la actual incorrecta responde 401")]
    public async Task CambiarContrasena_ActualIncorrecta_Lanza401()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("mala", user.PasswordHash)).Returns(PasswordCheck.Failed);

        await Assert.ThrowsAsync<UnauthorizedApiException>(() =>
            _sut.ChangePasswordAsync(10, new ChangePasswordDto { CurrentPassword = "mala", NewPassword = "Nueva#2026" }));

        _users.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact(DisplayName = "UT-PRF-08 Cambio de contrasena valido guarda el nuevo hash y audita PASSWORD_CHANGED")]
    public async Task CambiarContrasena_Valida_GuardaHash()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByIdAsync(10)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Actual#1", user.PasswordHash)).Returns(PasswordCheck.Ok);
        _hasher.Setup(h => h.Hash("Nueva#2026")).Returns("bcrypt:nueva");

        await _sut.ChangePasswordAsync(10, new ChangePasswordDto { CurrentPassword = "Actual#1", NewPassword = "Nueva#2026" });

        Assert.Equal("bcrypt:nueva", user.PasswordHash);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.PasswordChanged);
    }

    [Fact(DisplayName = "UT-PRF-09 Cambio de contrasena de usuario inexistente responde 404")]
    public async Task CambiarContrasena_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ChangePasswordAsync(10, new ChangePasswordDto { CurrentPassword = "a", NewPassword = "b" }));
    }
}
