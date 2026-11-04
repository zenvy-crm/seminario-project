using Moq;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Auth;
using Seminario.Application.Interfaces;
using Seminario.Application.Services;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;
using Seminario.UnitTests.Helpers;

namespace Seminario.UnitTests.Services;

[Trait("Category", "Unit")]
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenStore> _refreshTokens = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IMfaService> _mfa = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly List<AuditEvent> _auditoria = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(a => a.Enqueue(It.IsAny<AuditEvent>())).Callback<AuditEvent>(_auditoria.Add);

        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns(("jwt-de-prueba", DateTime.UtcNow.AddMinutes(30)));
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh-nuevo");
        _tokens.Setup(t => t.HashToken(It.IsAny<string>())).Returns<string>(raw => "hash:" + raw);
        _tokens.SetupGet(t => t.RefreshTokenExpirationDays).Returns(7);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => "bcrypt:" + p);

        _sut = new AuthService(_users.Object, _refreshTokens.Object, _tokens.Object, _mfa.Object, audit.Object, _hasher.Object);
    }

    private static RegisterRequestDto Registro() => new()
    {
        FirstName = "Ana",
        LastName = "Lopez",
        Username = "alopez",
        Email = "alopez@test.local",
        Password = "Segura#2026",
        ConfirmPassword = "Segura#2026",
        BirthDate = "2000-05-20"
    };

    // ---------- Registro ----------

    [Fact(DisplayName = "UT-AUTH-01 Registro rechaza un username duplicado (409)")]
    public async Task Registro_UsernameDuplicado_LanzaConflict()
    {
        _users.Setup(u => u.UsernameExistsAsync("alopez", null)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(Registro()));

        Assert.Equal(409, ex.StatusCode);
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-02 Registro rechaza un correo duplicado (409)")]
    public async Task Registro_EmailDuplicado_LanzaConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("alopez@test.local", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(Registro()));
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-03 Registro falla con 500 si el rol USER no existe")]
    public async Task Registro_SinRolPorDefecto_Lanza500()
    {
        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.RegisterAsync(Registro()));
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact(DisplayName = "UT-AUTH-04 Registro valido guarda el hash (nunca la contrasena) y audita USER_CREATED")]
    public async Task Registro_Valido_GuardaHashYAudita()
    {
        _users.Setup(u => u.GetRoleByNameAsync(RoleName.User)).ReturnsAsync(TestData.Rol());
        User? guardado = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => { u.Id = 99; guardado = u; });

        var (respuesta, userId) = await _sut.RegisterAsync(Registro());

        Assert.True(respuesta.Success);
        Assert.Equal(99, userId);
        Assert.NotNull(guardado);
        Assert.Equal("bcrypt:Segura#2026", guardado!.PasswordHash);
        Assert.Equal(new DateOnly(2000, 5, 20), guardado.BirthDate);
        Assert.Equal(UserStatus.ACTIVE, guardado.Status);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.UserCreated && a.UserId == 99);
    }

    [Fact(DisplayName = "UT-AUTH-05 Registro con fecha de nacimiento invalida la deja en null")]
    public async Task Registro_FechaInvalida_QuedaNull()
    {
        _users.Setup(u => u.GetRoleByNameAsync(RoleName.User)).ReturnsAsync(TestData.Rol());
        User? guardado = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => guardado = u);
        var dto = Registro();
        dto.BirthDate = "no-es-fecha";

        await _sut.RegisterAsync(dto);

        Assert.Null(guardado!.BirthDate);
    }

    // ---------- Login ----------

    [Fact(DisplayName = "UT-AUTH-06 Login con usuario inexistente responde 401 y audita LOGIN_FAILED")]
    public async Task Login_UsuarioInexistente_Lanza401()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedApiException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "nadie", Password = "x" }));

        Assert.Equal(401, ex.StatusCode);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.LoginFailed && a.Username == "nadie");
    }

    [Fact(DisplayName = "UT-AUTH-07 Login de una cuenta INACTIVE responde 403")]
    public async Task Login_CuentaInactiva_Lanza403()
    {
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(TestData.Usuario(status: UserStatus.INACTIVE));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "x" }));

        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.AccountDisabled);
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-08 Login de una cuenta bloqueada vigente responde 403 sin verificar la contrasena")]
    public async Task Login_CuentaBloqueada_Lanza403()
    {
        var user = TestData.Usuario();
        user.LockedUntil = DateTime.UtcNow.AddMinutes(3);
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "x" }));

        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-09 [Regresion] Bloqueo expirado se desbloquea solo y permite iniciar sesion")]
    [Trait("Category", "Regression")]
    public async Task Login_BloqueoExpirado_DesbloqueaAutomaticamente()
    {
        var user = TestData.Usuario();
        user.LockedUntil = DateTime.UtcNow.AddMinutes(-1);
        user.FailedLoginAttempts = 3;
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Correcta1", user.PasswordHash)).Returns(PasswordCheck.Ok);
        _mfa.Setup(m => m.GenerateAsync(user.Id)).ReturnsAsync("mfa-token");

        var resultado = await _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "Correcta1" });

        Assert.True(resultado.Response.RequiresMfa);
        Assert.Null(user.LockedUntil);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.AccountUnlocked);
    }

    [Fact(DisplayName = "UT-AUTH-10 Contrasena incorrecta incrementa el contador y responde 401")]
    public async Task Login_ContrasenaIncorrecta_IncrementaIntentos()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("mala", user.PasswordHash)).Returns(PasswordCheck.Failed);

        await Assert.ThrowsAsync<UnauthorizedApiException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "mala" }));

        Assert.Equal(1, user.FailedLoginAttempts);
        Assert.Null(user.LockedUntil);
        _users.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact(DisplayName = "UT-AUTH-11 [Regresion] Tercer intento fallido bloquea la cuenta 5 minutos (403)")]
    [Trait("Category", "Regression")]
    public async Task Login_TercerIntentoFallido_BloqueaCuenta()
    {
        var user = TestData.Usuario();
        user.FailedLoginAttempts = 2;
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("mala", user.PasswordHash)).Returns(PasswordCheck.Failed);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "mala" }));

        Assert.Contains("bloqueada", ex.Message);
        Assert.Equal(3, user.FailedLoginAttempts);
        Assert.NotNull(user.LockedUntil);
        Assert.InRange(user.LockedUntil!.Value, DateTime.UtcNow.AddMinutes(4), DateTime.UtcNow.AddMinutes(6));
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.AccountLocked);
    }

    [Fact(DisplayName = "UT-AUTH-12 Login correcto reinicia intentos y devuelve un desafio MFA")]
    public async Task Login_Correcto_ReiniciaIntentosYPideMfa()
    {
        var user = TestData.Usuario();
        user.FailedLoginAttempts = 2;
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Correcta1", user.PasswordHash)).Returns(PasswordCheck.Ok);
        _mfa.Setup(m => m.GenerateAsync(user.Id)).ReturnsAsync("mfa-token");

        var resultado = await _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "Correcta1" });

        Assert.True(resultado.Response.Success);
        Assert.True(resultado.Response.RequiresMfa);
        Assert.Equal("mfa-token", resultado.Response.Data.MfaToken);
        Assert.Null(resultado.Response.Data.AccessToken);
        Assert.Null(resultado.RefreshToken);
        Assert.Equal(0, user.FailedLoginAttempts);
        _users.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact(DisplayName = "UT-AUTH-13 Login con hash heredado lo regenera con BCrypt (PASSWORD_REHASHED)")]
    public async Task Login_HashHeredado_SeRehashea()
    {
        var user = TestData.Usuario(passwordHash: "AQAAAAEAACcQ-legacy");
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Correcta1", "AQAAAAEAACcQ-legacy")).Returns(PasswordCheck.NeedsRehash);
        _mfa.Setup(m => m.GenerateAsync(user.Id)).ReturnsAsync("mfa-token");

        await _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "Correcta1" });

        Assert.Equal("bcrypt:Correcta1", user.PasswordHash);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.PasswordRehashed);
    }

    [Fact(DisplayName = "UT-AUTH-14 Login correcto sin cambios pendientes no guarda en la base")]
    public async Task Login_SinCambios_NoGuarda()
    {
        var user = TestData.Usuario();
        _users.Setup(u => u.GetByUsernameAsync("jperez")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Correcta1", user.PasswordHash)).Returns(PasswordCheck.Ok);
        _mfa.Setup(m => m.GenerateAsync(user.Id)).ReturnsAsync("mfa-token");

        await _sut.LoginAsync(new LoginRequestDto { Username = "jperez", Password = "Correcta1" });

        _users.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ---------- MFA, refresh y logout ----------

    [Fact(DisplayName = "UT-AUTH-15 MFA valido emite access token, guarda el refresh hasheado y audita el login")]
    public async Task VerificarMfa_Valido_EmiteSesion()
    {
        var user = TestData.Usuario(rol: RoleName.Admin);
        _mfa.Setup(m => m.VerifyAsync("mfa-token", "123456")).ReturnsAsync(user);
        RefreshToken? guardado = null;
        _refreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Callback<RefreshToken>(t => guardado = t);

        var resultado = await _sut.VerifyMfaAsync(new MfaVerifyRequestDto { MfaToken = "mfa-token", Code = "123456" });

        Assert.Equal("jwt-de-prueba", resultado.Response.Data.AccessToken);
        Assert.Equal("refresh-nuevo", resultado.RefreshToken);
        Assert.Equal(RoleName.Admin, resultado.Response.Data.User!.Role);
        Assert.Equal("hash:refresh-nuevo", guardado!.TokenHash);
        Assert.NotNull(user.LastLoginAt);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.MfaSuccess);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.LoginSuccess);
    }

    [Theory(DisplayName = "UT-AUTH-16 Refresh sin cookie responde 401")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refresh_SinToken_Lanza401(string token)
    {
        await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.RefreshAsync(token));
    }

    [Fact(DisplayName = "UT-AUTH-17 Refresh con token revocado o expirado responde 401")]
    public async Task Refresh_TokenInexistente_Lanza401()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.RefreshAsync("viejo"));
        Assert.Contains("expiro", ex.Message);
    }

    [Fact(DisplayName = "UT-AUTH-18 Refresh valido rota el token: revoca el anterior y emite uno nuevo")]
    public async Task Refresh_Valido_RotaToken()
    {
        var anterior = new RefreshToken { Id = 1, UserId = 10, User = TestData.Usuario(), TokenHash = "hash:viejo" };
        _refreshTokens.Setup(r => r.GetActiveByHashAsync("hash:viejo")).ReturnsAsync(anterior);

        var resultado = await _sut.RefreshAsync("viejo");

        _refreshTokens.Verify(r => r.RevokeAsync(anterior), Times.Once);
        _refreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "hash:refresh-nuevo")), Times.Once);
        Assert.Equal("refresh-nuevo", resultado.RefreshToken);
    }

    [Fact(DisplayName = "UT-AUTH-19 Logout sin cookie no hace nada")]
    public async Task Logout_SinToken_NoHaceNada()
    {
        await _sut.LogoutAsync(null);
        _refreshTokens.Verify(r => r.GetActiveByHashAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-20 Logout revoca el refresh token y audita LOGOUT")]
    public async Task Logout_ConToken_Revoca()
    {
        var token = new RefreshToken { Id = 1, UserId = 10, User = TestData.Usuario(), TokenHash = "hash:abc" };
        _refreshTokens.Setup(r => r.GetActiveByHashAsync("hash:abc")).ReturnsAsync(token);

        await _sut.LogoutAsync("abc");

        _refreshTokens.Verify(r => r.RevokeAsync(token), Times.Once);
        _refreshTokens.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.Logout && a.UserId == 10);
    }

    [Fact(DisplayName = "UT-AUTH-21 Logout con token ya revocado no audita")]
    public async Task Logout_TokenInactivo_NoAudita()
    {
        await _sut.LogoutAsync("desconocido");
        Assert.DoesNotContain(_auditoria, a => a.EventType == AuditEventTypes.Logout);
    }
}
