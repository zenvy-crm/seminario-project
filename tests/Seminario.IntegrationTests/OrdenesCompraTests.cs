using System.Net;
using System.Net.Http.Json;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Operaciones;
using Seminario.IntegrationTests.Infrastructure;

namespace Seminario.IntegrationTests;

/// <summary>
/// Modulo Orden de Compra contra SQL Server real: SP_OrdenCompra_Crear, triggers de presupuesto,
/// soft delete y RBAC por niveles (Estandar / Intermedio / Administrador).
/// </summary>
[Collection(ApiCollection.Nombre)]
[Trait("Category", "Integration")]
public class OrdenesCompraTests
{
    private const string Bodega = "GT-CENTRO";
    private readonly ApiFactory _api;

    public OrdenesCompraTests(ApiFactory api) => _api = api;

    private static string NitUnico() => Random.Shared.Next(10_000_000, 99_999_999).ToString() + "-K";

    private static object Orden(string nit, params object[] lineas) => new
    {
        proveedorNit = nit,
        proveedorNombre = "Proveedor de Pruebas S.A.",
        moneda = "GTQ",
        bodegaDestino = Bodega,
        detalle = "Orden creada por pruebas de integracion",
        lineas
    };

    private async Task<OrdenCompraDto> CrearOrdenAsync(HttpClient client, string nit)
    {
        var response = await client.PostAsJsonAsync("/api/operaciones/ordenes", Orden(nit,
            new { descripcion = "Tarimas de madera", cantidad = 10, precioUnitario = 125.50m },
            new { descripcion = "Film stretch", cantidad = 4, precioUnitario = 80m }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.LeerAsync<OrdenCompraDto>();
    }

    private static async Task<decimal> ComprometidoAsync(HttpClient client)
    {
        var presupuestos = await client.GetFromJsonAsync<List<PresupuestoBodegaDto>>($"/api/operaciones/presupuestos?anio={DateTime.UtcNow.Year}");
        return presupuestos!.Single(p => p.Bodega == Bodega).MontoComprometido;
    }

    [Fact(DisplayName = "IT-OC-01 Crear orden con lineas: nace CREADA, el total lo calcula el trigger y compromete presupuesto")]
    public async Task CrearOrden_ComprometePresupuesto()
    {
        var estandar = await _api.ClienteComo("estandar");
        var antes = await ComprometidoAsync(estandar);

        var orden = await CrearOrdenAsync(estandar, NitUnico());

        Assert.Equal("CREADA", orden.Estado);
        Assert.Matches(@"^OC-\d{4}-\d{6}$", orden.NumeroOrden);
        Assert.Equal(1575.00m, orden.Monto);
        Assert.Equal(2, orden.Lineas.Count);
        Assert.All(orden.Lineas, l => Assert.True(l.EstadoLinea));
        Assert.Equal(antes + 1575.00m, await ComprometidoAsync(estandar));
    }

    [Fact(DisplayName = "IT-OC-02 Orden solo con Monto crea una unica linea 'Monto global'")]
    public async Task CrearOrden_SoloMonto()
    {
        var estandar = await _api.ClienteComo("estandar");

        var response = await estandar.PostAsJsonAsync("/api/operaciones/ordenes", new
        {
            proveedorNit = NitUnico(),
            proveedorNombre = "Proveedor Monto",
            moneda = "USD",
            bodegaDestino = Bodega,
            monto = 250.75m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var orden = await response.LeerAsync<OrdenCompraDto>();
        var linea = Assert.Single(orden.Lineas);
        Assert.Equal("Monto global", linea.Descripcion);
        Assert.Equal(250.75m, orden.Monto);
    }

    [Fact(DisplayName = "IT-OC-03 [Regresion] Linea invalida revierte TODA la orden (rollback absoluto del SP)")]
    [Trait("Category", "Regression")]
    public async Task LineaInvalida_RollbackAbsoluto()
    {
        var estandar = await _api.ClienteComo("estandar");
        var nit = NitUnico();

        var response = await estandar.PostAsJsonAsync("/api/operaciones/ordenes", Orden(nit,
            new { descripcion = "Linea valida", cantidad = 1, precioUnitario = 10m },
            new { descripcion = "Linea con cantidad cero", cantidad = 0, precioUnitario = 10m }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var listado = await estandar.GetFromJsonAsync<PagedResult<OrdenCompraDto>>($"/api/operaciones/ordenes?proveedorNit={nit}");
        Assert.Equal(0, listado!.Total);
    }

    [Theory(DisplayName = "IT-OC-04 Validaciones de la orden responden 422")]
    [InlineData("SIN-PRESUPUESTO", "GTQ", 0)]
    [InlineData(Bodega, "EUR", 0)]
    [InlineData(Bodega, "GTQ", 30)]
    public async Task Validaciones_422(string bodega, string moneda, int diasFuturo)
    {
        var estandar = await _api.ClienteComo("estandar");

        var response = await estandar.PostAsJsonAsync("/api/operaciones/ordenes", new
        {
            proveedorNit = NitUnico(),
            proveedorNombre = "Proveedor Invalido",
            moneda,
            bodegaDestino = bodega,
            fechaEmision = DateTime.UtcNow.Date.AddDays(diasFuturo),
            lineas = new[] { new { descripcion = "Item", cantidad = 1, precioUnitario = 5m } }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "IT-OC-05 Orden sin lineas ni monto y NIT con formato invalido responden 422")]
    public async Task SinLineas_YNitInvalido_422()
    {
        var estandar = await _api.ClienteComo("estandar");

        var sinLineas = await estandar.PostAsJsonAsync("/api/operaciones/ordenes", new
        {
            proveedorNit = NitUnico(),
            proveedorNombre = "Proveedor",
            moneda = "GTQ",
            bodegaDestino = Bodega
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinLineas.StatusCode);

        var nitInvalido = await estandar.PostAsJsonAsync("/api/operaciones/ordenes", Orden("NIT<script>",
            new { descripcion = "Item", cantidad = 1, precioUnitario = 5m }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nitInvalido.StatusCode);
    }

    [Fact(DisplayName = "IT-OC-06 RBAC: Estandar no anula lineas (403) e Intermedio no anula ordenes (403)")]
    public async Task Rbac_PorNivel()
    {
        var estandar = await _api.ClienteComo("estandar");
        var intermedio = await _api.ClienteComo("intermedio");
        var orden = await CrearOrdenAsync(estandar, NitUnico());

        var lineaPorEstandar = await estandar.PutAsJsonAsync($"/api/operaciones/ordenes/{orden.Id}/lineas/1", new { motivo = "No autorizado" });
        Assert.Equal(HttpStatusCode.Forbidden, lineaPorEstandar.StatusCode);

        var ordenPorIntermedio = await intermedio.DeleteAsync($"/api/operaciones/ordenes/{orden.Id}?motivo=No%20autorizado");
        Assert.Equal(HttpStatusCode.Forbidden, ordenPorIntermedio.StatusCode);
    }

    [Fact(DisplayName = "IT-OC-07 Intermedio anula una linea: el trigger recalcula el total y libera presupuesto")]
    public async Task AnularLinea_RecalculaTotal()
    {
        var estandar = await _api.ClienteComo("estandar");
        var intermedio = await _api.ClienteComo("intermedio");
        var orden = await CrearOrdenAsync(estandar, NitUnico());
        var antes = await ComprometidoAsync(estandar);

        var corto = await intermedio.PutAsJsonAsync($"/api/operaciones/ordenes/{orden.Id}/lineas/2", new { motivo = " x " });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corto.StatusCode);

        var response = await intermedio.PutAsJsonAsync($"/api/operaciones/ordenes/{orden.Id}/lineas/2", new { motivo = "Proveedor sin existencias" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizada = await response.LeerAsync<OrdenCompraDto>();

        Assert.Equal(1255.00m, actualizada.Monto);
        Assert.False(actualizada.Lineas.Single(l => l.Linea == 2).EstadoLinea);
        Assert.Equal("Proveedor sin existencias", actualizada.Lineas.Single(l => l.Linea == 2).MotivoBaja);
        Assert.Equal(antes - 320.00m, await ComprometidoAsync(estandar));

        var otraVez = await intermedio.PutAsJsonAsync($"/api/operaciones/ordenes/{orden.Id}/lineas/2", new { motivo = "Proveedor sin existencias" });
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);

        var inexistente = await intermedio.PutAsJsonAsync($"/api/operaciones/ordenes/{orden.Id}/lineas/99", new { motivo = "Linea inexistente" });
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact(DisplayName = "IT-OC-08 [Regresion] Orden ANULADA por el admin desaparece para el rol Estandar (soft delete)")]
    [Trait("Category", "Regression")]
    public async Task AnularOrden_SoftDelete()
    {
        var estandar = await _api.ClienteComo("estandar");
        var admin = await _api.ClienteComo("admin");
        var nit = NitUnico();
        var orden = await CrearOrdenAsync(estandar, nit);

        var anulada = await admin.DeleteAsync($"/api/operaciones/ordenes/{orden.Id}?motivo=Duplicada%20por%20error");
        Assert.Equal(HttpStatusCode.OK, anulada.StatusCode);
        var dto = await anulada.LeerAsync<OrdenCompraDto>();
        Assert.Equal("ANULADA", dto.Estado);
        Assert.Equal("Duplicada por error", dto.MotivoAnulacion);

        Assert.Equal(HttpStatusCode.NotFound, (await estandar.GetAsync($"/api/operaciones/ordenes/{orden.Id}")).StatusCode);
        var listaEstandar = await estandar.GetFromJsonAsync<PagedResult<OrdenCompraDto>>($"/api/operaciones/ordenes?proveedorNit={nit}&estado=ANULADA");
        Assert.Equal(0, listaEstandar!.Total);

        var paraAdmin = await admin.GetFromJsonAsync<OrdenCompraDto>($"/api/operaciones/ordenes/{orden.Id}?fechaEmision={orden.FechaEmision:yyyy-MM-dd}");
        Assert.Equal("ANULADA", paraAdmin!.Estado);

        var otraVez = await admin.DeleteAsync($"/api/operaciones/ordenes/{orden.Id}?motivo=Segunda%20vez");
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
    }

    [Fact(DisplayName = "IT-OC-09 Bloqueo optimista: un rowVer desactualizado responde 409")]
    public async Task RowVerDesactualizado_409()
    {
        var estandar = await _api.ClienteComo("estandar");
        var admin = await _api.ClienteComo("admin");
        var orden = await CrearOrdenAsync(estandar, NitUnico());

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/operaciones/ordenes/{orden.Id}")
        {
            Content = JsonContent.Create(new { motivo = "Cambio concurrente", rowVer = Convert.ToBase64String(new byte[8]) })
        };
        var response = await admin.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "IT-OC-10 Listado paginado con filtros, consulta por id y particiones de la tabla")]
    public async Task Listado_YParticiones()
    {
        var estandar = await _api.ClienteComo("estandar");
        var nit = NitUnico();
        var orden = await CrearOrdenAsync(estandar, nit);
        var hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var listado = await estandar.GetFromJsonAsync<PagedResult<OrdenCompraDto>>(
            $"/api/operaciones/ordenes?desde={hoy}&hasta={hoy}&proveedorNit={nit}&soloMias=true&conDetalle=true&numeroOrden={orden.NumeroOrden}&page=0&pageSize=500");
        Assert.Equal(1, listado!.Total);
        Assert.Equal(20, listado.PageSize);

        var detalle = await estandar.GetFromJsonAsync<OrdenCompraDto>($"/api/operaciones/ordenes/{orden.Id}");
        Assert.NotNull(detalle!.Presupuesto);

        Assert.Equal(HttpStatusCode.NotFound, (await estandar.GetAsync($"/api/operaciones/ordenes/{Guid.NewGuid()}")).StatusCode);

        var particiones = await estandar.GetFromJsonAsync<List<ParticionInfoDto>>("/api/operaciones/particiones");
        Assert.NotEmpty(particiones!);
    }
}
