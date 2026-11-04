using Microsoft.EntityFrameworkCore;
using Moq;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Infrastructure.Auth;
using Seminario.Infrastructure.Persistence;
using Seminario.UnitTests.Helpers;

namespace Seminario.UnitTests.Infrastructure;

/// <summary>MfaService contra EF Core InMemory: valida la logica del codigo temporal sin SQL Server.</summary>
[Trait("Category", "Unit")]
public sealed class MfaServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CapturingLogger<MfaService> _logger = new();
    private readonly List<AuditEvent> _auditoria = new();
    private readonly MfaService _sut;

    public MfaServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("mfa-" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);

        var user = TestData.Usuario(id: 10);
        _db.Roles.Add(user.Role);
        _db.Users.Add(user);
        _db.SaveChanges();

        var audit = new Mock<IAuditLogger>();
        audit.Setup(a => a.Enqueue(It.IsAny<AuditEvent>())).Callback<AuditEvent>(_auditoria.Add);
        _sut = new MfaService(_db, audit.Object, _logger);
    }

    public void Dispose() => _db.Dispose();

    private string CodigoEmitido() => (string)_logger.Entries.Last().Values["Code"]!;

    [Fact(DisplayName = "UT-MFA-01 Generar guarda el codigo hasheado (nunca en claro) con vigencia de 5 minutos")]
    public async Task Generar_GuardaHash()
    {
        var token = await _sut.GenerateAsync(10);

        var guardado = await _db.MfaCodes.SingleAsync();
        var codigo = CodigoEmitido();
        Assert.Equal(64, token.Length);
        Assert.Equal(token, guardado.MfaToken);
        Assert.Matches("^[0-9]{6}$", codigo);
        Assert.NotEqual(codigo, guardado.CodeHash);
        Assert.InRange(guardado.ExpiresAt, DateTime.UtcNow.AddMinutes(4), DateTime.UtcNow.AddMinutes(6));
    }

    [Fact(DisplayName = "UT-MFA-02 Codigo correcto devuelve el usuario y lo marca como usado")]
    public async Task Verificar_CodigoCorrecto_DevuelveUsuario()
    {
        var token = await _sut.GenerateAsync(10);

        var user = await _sut.VerifyAsync(token, CodigoEmitido());

        Assert.Equal(10, user.Id);
        Assert.True((await _db.MfaCodes.SingleAsync()).IsUsed);
    }

    [Fact(DisplayName = "UT-MFA-03 [Regresion] Reusar un codigo ya usado responde 401 y audita MFA_FAILED")]
    [Trait("Category", "Regression")]
    public async Task Verificar_CodigoReusado_Lanza401()
    {
        var token = await _sut.GenerateAsync(10);
        var codigo = CodigoEmitido();
        await _sut.VerifyAsync(token, codigo);

        await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.VerifyAsync(token, codigo));
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.MfaFailed && a.UserId == 10);
    }

    [Fact(DisplayName = "UT-MFA-04 Token MFA inexistente responde 401")]
    public async Task Verificar_TokenInexistente_Lanza401()
    {
        await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.VerifyAsync("no-existe", "000000"));
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.MfaFailed && a.UserId == null);
    }

    [Fact(DisplayName = "UT-MFA-05 [Regresion] Codigo expirado responde 401 y audita MFA_FAILED")]
    [Trait("Category", "Regression")]
    public async Task Verificar_CodigoExpirado_Lanza401()
    {
        var token = await _sut.GenerateAsync(10);
        var mfa = await _db.MfaCodes.SingleAsync();
        mfa.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.VerifyAsync(token, CodigoEmitido()));
        Assert.Contains(_auditoria, a => a.EventType == AuditEventTypes.MfaFailed);
    }

    [Fact(DisplayName = "UT-MFA-06 Codigo incorrecto incrementa los intentos")]
    public async Task Verificar_CodigoIncorrecto_IncrementaIntentos()
    {
        var token = await _sut.GenerateAsync(10);
        var incorrecto = CodigoEmitido() == "000000" ? "111111" : "000000";

        var ex = await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.VerifyAsync(token, incorrecto));

        Assert.Contains("incorrecto", ex.Message);
        Assert.Equal(1, (await _db.MfaCodes.SingleAsync()).Attempts);
    }

    [Fact(DisplayName = "UT-MFA-07 Superar el maximo de intentos bloquea el codigo aunque sea correcto")]
    public async Task Verificar_MaximoIntentos_Lanza401()
    {
        var token = await _sut.GenerateAsync(10);
        var mfa = await _db.MfaCodes.SingleAsync();
        mfa.Attempts = MfaCode.MaxAttempts;
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<UnauthorizedApiException>(() => _sut.VerifyAsync(token, CodigoEmitido()));
        Assert.Contains("maximo", ex.Message);
    }
}
