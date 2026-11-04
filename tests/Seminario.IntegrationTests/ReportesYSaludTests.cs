using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Seminario.Application.DTOs.Reportes;
using Seminario.IntegrationTests.Infrastructure;

namespace Seminario.IntegrationTests;

[Collection(ApiCollection.Nombre)]
[Trait("Category", "Integration")]
public class ReportesTests
{
    private readonly ApiFactory _api;

    public ReportesTests(ApiFactory api) => _api = api;

    [Fact(DisplayName = "IT-REP-01 Ranking de proveedores del anio incluye a un proveedor con compras")]
    public async Task Ranking_IncluyeProveedor()
    {
        var estandar = await _api.ClienteComo("estandar");
        var nit = Random.Shared.Next(10_000_000, 99_999_999) + "-1";
        var creada = await estandar.PostAsJsonAsync("/api/operaciones/ordenes", new
        {
            proveedorNit = nit,
            proveedorNombre = "Proveedor Ranking",
            moneda = "GTQ",
            bodegaDestino = "GT-NORTE",
            lineas = new[] { new { descripcion = "Item ranking", cantidad = 1, precioUnitario = 99_000m } }
        });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);

        var ranking = await estandar.GetFromJsonAsync<List<RankingProveedorDto>>($"/api/reportes/ranking-proveedores?anio={DateTime.UtcNow.Year}&top=100");

        var fila = Assert.Single(ranking!, r => r.ProveedorNit == nit);
        Assert.Equal(99_000m, fila.TotalComprometido);
        Assert.True(fila.PosicionRanking >= 1);
    }

    [Theory(DisplayName = "IT-REP-02 Ranking con anio o top fuera de rango responde 422")]
    [InlineData("anio=2000")]
    [InlineData("top=0")]
    [InlineData("top=101")]
    public async Task Ranking_FueraDeRango_422(string query)
    {
        var estandar = await _api.ClienteComo("estandar");

        var response = await estandar.GetAsync($"/api/reportes/ranking-proveedores?{query}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory(DisplayName = "IT-REP-03 Reporte de ordenes exige un rango de fechas valido (400)")]
    [InlineData("")]
    [InlineData("fechaDesde=2026-05-10&fechaHasta=2026-05-01")]
    [InlineData("fechaDesde=2024-01-01&fechaHasta=2026-01-01")]
    public async Task ReporteOrdenes_RangoInvalido_400(string query)
    {
        var estandar = await _api.ClienteComo("estandar");

        var response = await estandar.GetAsync($"/api/reportes/ordenes?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "IT-REP-04 Reporte de ordenes del dia devuelve filas y el monto total")]
    public async Task ReporteOrdenes_Valido()
    {
        var admin = await _api.ClienteComo("admin");
        var hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var reporte = await admin.GetFromJsonAsync<ReporteOrdenesResultDto>(
            $"/api/reportes/ordenes?fechaDesde={hoy}&fechaHasta={hoy}&estado=CREADA&bodega=GT-CENTRO&page=1&pageSize=10");

        Assert.NotNull(reporte);
        Assert.True(reporte!.MontoTotal >= 0);
        Assert.True(reporte.Items.Count <= 10);
    }

    [Theory(DisplayName = "IT-REP-05 Auditoria de soft delete: solo el Administrador (por tipo)")]
    [InlineData("ordenes")]
    [InlineData("lineas")]
    [InlineData("documentos")]
    public async Task AuditoriaSoftDelete_Admin(string tipo)
    {
        var admin = await _api.ClienteComo("admin");

        var response = await admin.GetAsync($"/api/reportes/auditoria/soft-delete?tipo={tipo}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.LeerAsync<AuditoriaSoftDeleteResultDto>();
        Assert.NotNull(resultado.Resumen);
    }

    [Fact(DisplayName = "IT-REP-06 Auditoria de soft delete: Estandar recibe 403 y un tipo invalido 422")]
    public async Task AuditoriaSoftDelete_Errores()
    {
        var estandar = await _api.ClienteComo("estandar");
        var admin = await _api.ClienteComo("admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await estandar.GetAsync("/api/reportes/auditoria/soft-delete")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync("/api/reportes/auditoria/soft-delete?tipo=otro")).StatusCode);
    }
}

[Collection(ApiCollection.Nombre)]
[Trait("Category", "Integration")]
public class SaludTests
{
    private readonly ApiFactory _api;

    public SaludTests(ApiFactory api) => _api = api;

    [Fact(DisplayName = "IT-HLT-01 /api/health/live responde 200 sin autenticacion")]
    public async Task Liveness()
    {
        var response = await _api.Cliente().GetAsync("/api/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "IT-HLT-02 /api/health reporta SQL Server y MongoDB como Healthy")]
    public async Task Readiness()
    {
        var response = await _api.Cliente().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        var checks = json.RootElement.GetProperty("checks").EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());
        Assert.Equal("Healthy", checks["sqlserver"]);
        Assert.Equal("Healthy", checks["mongodb"]);
    }

    [Fact(DisplayName = "IT-HLT-03 Swagger publica el contrato OpenAPI en Development")]
    public async Task Swagger()
    {
        var response = await _api.Cliente().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
