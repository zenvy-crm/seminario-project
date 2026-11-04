using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Seminario.Api;
using Seminario.Api.Middleware;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.Interfaces;
using Seminario.Application.Mappings;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.MongoDB;
using Seminario.UnitTests.Helpers;

namespace Seminario.UnitTests.Api;

[Trait("Category", "Unit")]
public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, ApiErrorResponse Body)> Ejecutar(RequestDelegate siguiente)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(siguiente, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return (context.Response.StatusCode, body!);
    }

    [Fact(DisplayName = "UT-API-01 ApiException se traduce a su codigo HTTP con cuerpo JSON uniforme")]
    public async Task ApiException_SeTraduce()
    {
        var (status, body) = await Ejecutar(_ =>
            throw new ValidationApiException("Datos invalidos", new List<string> { "campo" }));

        Assert.Equal(422, status);
        Assert.False(body.Success);
        Assert.Equal("Datos invalidos", body.Message);
        Assert.Equal(new[] { "campo" }, body.Errors);
    }

    [Fact(DisplayName = "UT-API-02 Excepcion no controlada responde 500 sin filtrar detalles internos")]
    public async Task ExcepcionGenerica_Responde500()
    {
        var (status, body) = await Ejecutar(_ => throw new InvalidOperationException("cadena de conexion secreta"));

        Assert.Equal(500, status);
        Assert.DoesNotContain("secreta", body.Message);
    }

    [Fact(DisplayName = "UT-API-03 Sin excepcion el middleware deja pasar la respuesta")]
    public async Task SinExcepcion_PasaDeLargo()
    {
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(c => { c.Response.StatusCode = 204; return Task.CompletedTask; },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(204, context.Response.StatusCode);
    }

    [Theory(DisplayName = "UT-API-04 Cada excepcion de dominio tiene su codigo HTTP")]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(UnauthorizedApiException), 401)]
    public void ExcepcionesDeDominio_TienenCodigo(Type tipo, int esperado)
    {
        var ex = (ApiException)Activator.CreateInstance(tipo, "mensaje")!;
        Assert.Equal(esperado, ex.StatusCode);
        Assert.Empty(ex.Errors);
    }

    [Fact(DisplayName = "UT-API-05 ConflictException conserva la excepcion original")]
    public void Conflict_ConservaInner()
    {
        var inner = new InvalidOperationException("concurrencia");
        var ex = new ConflictException("conflicto", inner);
        Assert.Same(inner, ex.InnerException);
        Assert.Equal(409, ex.StatusCode);
    }
}

[Trait("Category", "Unit")]
public class RbacTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    [Theory(DisplayName = "UT-RBAC-01 Nivel de cada rol (1 Estandar, 2 Intermedio, 3 Administrador)")]
    [InlineData("USER", 1, "Estandar")]
    [InlineData("intermedio", 2, "Intermedio")]
    [InlineData("ADMIN", 3, "Administrador")]
    [InlineData("OTRO", 0, "OTRO")]
    [InlineData(null, 0, "")]
    public void NivelYNombreDeRol(string? rol, int nivel, string rolAuth)
    {
        Assert.Equal(nivel, RoleName.NivelDe(rol));
        Assert.Equal(rolAuth, RoleName.RolAuthDe(rol));
    }

    [Fact(DisplayName = "UT-RBAC-02 [Seguridad] El claim nivel nunca eleva por encima del nivel del rol")]
    [Trait("Category", "Regression")]
    public void ClaimNivel_NoEscalaPrivilegios()
    {
        var usuario = Principal(new Claim(ClaimTypes.Role, RoleName.User), new Claim("nivel", "3"));
        Assert.Equal(1, usuario.GetNivel());
    }

    [Fact(DisplayName = "UT-RBAC-03 Token sin claim nivel deriva el nivel del rol")]
    public void SinClaimNivel_DerivaDelRol()
    {
        var usuario = Principal(new Claim("role", RoleName.Intermedio));
        Assert.Equal(2, usuario.GetNivel());
        Assert.Equal(RoleName.Intermedio, usuario.GetRoleName());
    }

    [Fact(DisplayName = "UT-RBAC-04 La sesion se arma con id, username, rol y nivel del JWT")]
    public void ToUsuarioSesion_ArmaSesion()
    {
        var usuario = Principal(
            new Claim("sub", "21"),
            new Claim("username", "estandar"),
            new Claim(ClaimTypes.Role, RoleName.User),
            new Claim("nivel", "1"));

        var sesion = usuario.ToUsuarioSesion();

        Assert.Equal(new UsuarioSesion(21, "estandar", RoleName.User, 1), sesion);
        Assert.True(sesion.EsEstandar);
        Assert.False(usuario.IsAdmin());
    }

    [Fact(DisplayName = "UT-RBAC-05 Token sin usuario valido responde 403")]
    public void ToUsuarioSesion_SinUsuario_Lanza403()
    {
        var usuario = Principal(new Claim(ClaimTypes.Role, RoleName.Admin));
        Assert.Throws<ForbiddenException>(() => usuario.ToUsuarioSesion());
    }

    [Fact(DisplayName = "UT-RBAC-06 GetUserId lee NameIdentifier y el username cae a ClaimTypes.Name")]
    public void GetUserId_DesdeNameIdentifier()
    {
        var usuario = Principal(
            new Claim(ClaimTypes.NameIdentifier, "8"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, RoleName.Admin));

        Assert.Equal(8, usuario.GetUserId());
        Assert.Equal("admin", usuario.GetUsername());
        Assert.True(usuario.IsAdmin());
        Assert.False(new UsuarioSesion(8, "admin", RoleName.Admin, 3).EsEstandar);
    }

    [Fact(DisplayName = "UT-RBAC-07 Id no numerico se interpreta como 0")]
    public void GetUserId_NoNumerico_Cero()
    {
        Assert.Equal(0, Principal(new Claim("sub", "abc")).GetUserId());
    }
}

[Trait("Category", "Unit")]
public class UserMappingsTests
{
    [Fact(DisplayName = "UT-MAP-01 IsLocked solo es true mientras el bloqueo esta vigente")]
    public void IsLocked_SegunLockedUntil()
    {
        var user = TestData.Usuario();
        Assert.False(user.ToDto().IsLocked);

        user.LockedUntil = DateTime.UtcNow.AddMinutes(2);
        Assert.True(user.ToDto().IsLocked);

        user.LockedUntil = DateTime.UtcNow.AddMinutes(-2);
        Assert.False(user.ToDto().IsLocked);
    }

    [Fact(DisplayName = "UT-MAP-02 El DTO expone la URL de descarga de cada documento y nunca el hash")]
    public void Documentos_TienenUrl()
    {
        var user = TestData.Usuario(id: 4);
        user.Documents.Add(new Seminario.Domain.Entities.UserDocument
        {
            Id = 9, UserId = 4, FileName = "dpi.pdf", ContentType = "application/pdf", FileSize = 10
        });

        var dto = user.ToDto();

        var doc = Assert.Single(dto.Documents!);
        Assert.Equal("/api/users/4/documents/9/content", doc.Url);
        Assert.Equal("1999-01-15", dto.BirthDate);
        Assert.DoesNotContain(typeof(Seminario.Application.DTOs.UserDto).GetProperties(), p => p.Name.Contains("Password"));
    }
}

[Trait("Category", "Unit")]
public class AuditChannelTests
{
    private static AuditChannel Canal(Mock<IAuditStore> store, HttpContext? context = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        return new AuditChannel(accessor.Object, store.Object, NullLogger<AuditChannel>.Instance);
    }

    [Fact(DisplayName = "UT-AUD-01 Enqueue completa ip (X-Forwarded-For), ejecutor y rol desde el JWT")]
    public async Task Enqueue_CompletaDesdeContexto()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "10.1.2.3, 172.16.0.1";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "5"),
            new Claim("username", "admin"),
            new Claim(ClaimTypes.Role, RoleName.Admin)
        }, "jwt"));
        var canal = Canal(new Mock<IAuditStore>(), context);

        canal.Enqueue(new AuditEvent { EventType = AuditEventTypes.UserUpdated, Entity = "User" });

        var evento = await canal.Reader.ReadAsync();
        Assert.Equal("10.1.2.3", evento.IpAddress);
        Assert.Equal(5, evento.ActorUserId);
        Assert.Equal("admin", evento.ActorUsername);
        Assert.Equal("Administrador", evento.RolAuth);
    }

    [Fact(DisplayName = "UT-AUD-02 Enqueue fuera de una peticion HTTP no falla")]
    public async Task Enqueue_SinContexto()
    {
        var canal = Canal(new Mock<IAuditStore>());

        canal.Enqueue(new AuditEvent { EventType = AuditEventTypes.LoginFailed, Entity = "User" });

        var evento = await canal.Reader.ReadAsync();
        Assert.Null(evento.IpAddress);
        Assert.Null(evento.ActorUserId);
    }

    [Fact(DisplayName = "UT-AUD-03 FireAndForget reintenta cuando MongoDB falla y termina guardando")]
    public async Task FireAndForget_Reintenta()
    {
        var store = new Mock<IAuditStore>();
        var guardado = new TaskCompletionSource<bool>();
        store.SetupSequence(s => s.InsertAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("mongo caido"))
            .Returns(() => { guardado.TrySetResult(true); return Task.CompletedTask; });

        Canal(store).FireAndForget(new AuditEvent { EventType = AuditEventTypes.AnulacionMaster, Entity = "ORDEN_COMPRA" });

        var termino = await Task.WhenAny(guardado.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(guardado.Task, termino);
        store.Verify(s => s.InsertAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "UT-AUD-04 MongoAuditStore.ToDocument normaliza la fecha a UTC y omite textos vacios")]
    public void ToDocument_Normaliza()
    {
        var evento = new AuditEvent
        {
            EventType = AuditEventTypes.LineaAnulada,
            Entity = "ORDEN_COMPRA",
            EntityId = "OC-2026-000001#1",
            RolAuth = " ",
            Justificacion = "Error de digitacion",
            Before = new { linea = 1 },
            Timestamp = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified)
        };

        var doc = MongoAuditStore.ToDocument(evento);

        Assert.Equal(DateTimeKind.Utc, doc.Timestamp.Kind);
        Assert.Null(doc.RolAuth);
        Assert.Equal("Error de digitacion", doc.Justificacion);
        Assert.Equal(1, doc.Before!["linea"].AsInt32);
        Assert.Null(doc.After);
    }
}
