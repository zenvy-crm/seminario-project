using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seminario.Application.Audit;
using Seminario.Application.DTOs.Operaciones;
using Seminario.Application.Interfaces;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.Logging;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Services;

/// <summary>
/// Inyeccion masiva de ordenes historicas para probar el particionamiento.
/// </summary>
public class SeedDataService : ISeedDataService
{
    private const int BatchSize = 5_000;

    private static readonly string[] Bodegas =
    {
        "Bodega Central Zona 12", "Centro de Distribucion Villa Nueva", "Bodega Norte Chimaltenango",
        "Bodega Regional Quetzaltenango", "Cross-dock Puerto Barrios"
    };

    private static readonly string[] Proveedores =
    {
        "Alimentos del Pacifico, S.A.", "Distribuidora Maya, S.A.", "Corporacion Xelaju, S.A.",
        "Importadora Atlantico, S.A.", "Granos y Cereales del Norte, S.A.", "Lacteos La Cumbre, S.A.",
        "Plasticos Industriales, S.A.", "Bebidas del Istmo, S.A."
    };

    // Las filas del stress test son historicas: no pasan por el SP ni los triggers, asi que no
    // tienen lineas ni compromiso de presupuesto. Por eso nunca nacen CREADA (el Monto no sale
    // de lineas) ni ANULADA (no habria evidencia de anulacion ni en SQL ni en MongoDB).
    private static readonly string[] EstadosHistoricos =
    {
        OrdenEstado.Emitida, OrdenEstado.Recibida, OrdenEstado.Pagada
    };

    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly IOrdenCompraService _ordenCompraService;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        AppDbContext db,
        IAuditLogger auditLogger,
        IOrdenCompraService ordenCompraService,
        ILogger<SeedDataService> logger)
    {
        _db = db;
        _auditLogger = auditLogger;
        _ordenCompraService = ordenCompraService;
        _logger = logger;
    }

    public async Task<SeedDataResultDto> InyectarAsync(int userId, SeedDataRequestDto request, CancellationToken cancellationToken)
    {
        if (request.AnioFinal < request.AnioInicial)
        {
            throw new Application.Common.ValidationApiException("Datos invalidos",
                new List<string> { "El año final no puede ser menor al anio inicial" });
        }

        var cronometro = Stopwatch.StartNew();
        var random = new Random();
        var inicio = new DateTime(request.AnioInicial, 1, 1);
        var diasRango = (new DateTime(request.AnioFinal, 12, 31) - inicio).Days + 1;

        var tabla = CrearEsquemaTabla();
        var insertados = 0;
        var lotes = 0;

        await using var connection = new SqlConnection(_db.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "TB_Ordenes_Compra",
            BatchSize = BatchSize,
            BulkCopyTimeout = 600
        };

        foreach (DataColumn columna in tabla.Columns)
        {
            bulkCopy.ColumnMappings.Add(columna.ColumnName, columna.ColumnName);
        }

        while (insertados < request.Cantidad)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var enEsteLote = Math.Min(BatchSize, request.Cantidad - insertados);
            tabla.Clear();

            for (var i = 0; i < enEsteLote; i++)
            {
                var fecha = inicio.AddDays(random.Next(diasRango));
                var fila = tabla.NewRow();

                fila["Id"] = Guid.NewGuid();
                fila["FechaEmision"] = fecha;
                fila["NumeroOrden"] = $"OC-{fecha:yyyyMM}-{random.Next(0, 99999999):D8}";
                fila["UserId"] = userId;
                fila["ProveedorNit"] = $"{random.Next(1000000, 99999999)}-{random.Next(0, 9)}";
                fila["ProveedorNombre"] = Proveedores[random.Next(Proveedores.Length)];
                fila["Monto"] = Math.Round((decimal)(random.NextDouble() * 49_900 + 100), 2);
                fila["Moneda"] = random.Next(10) == 0 ? "USD" : "GTQ";
                fila["Estado"] = EstadosHistoricos[random.Next(EstadosHistoricos.Length)];
                fila["BodegaDestino"] = Bodegas[random.Next(Bodegas.Length)];
                fila["Detalle"] = "Orden generada por el stress test de particionamiento";
                fila["CreatedAt"] = DateTime.UtcNow;

                tabla.Rows.Add(fila);
            }

            await bulkCopy.WriteToServerAsync(tabla, cancellationToken);

            insertados += enEsteLote;
            lotes++;

            _logger.StressTestAvance(insertados, request.Cantidad, lotes);
        }

        cronometro.Stop();

        var resultado = new SeedDataResultDto
        {
            RegistrosInsertados = insertados,
            DuracionMs = cronometro.ElapsedMilliseconds,
            Lotes = lotes,
            RegistrosPorSegundo = cronometro.Elapsed.TotalSeconds > 0
                ? Math.Round(insertados / cronometro.Elapsed.TotalSeconds, 1)
                : insertados,
            Particiones = await _ordenCompraService.GetParticionesAsync()
        };

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.SeedDataEjecutado,
            Entity = "OrdenCompra",
            UserId = userId,
            After = new
            {
                resultado.RegistrosInsertados,
                resultado.DuracionMs,
                resultado.Lotes,
                rango = $"{request.AnioInicial}-{request.AnioFinal}"
            }
        });

        return resultado;
    }

    private static DataTable CrearEsquemaTabla()
    {
        var tabla = new DataTable();
        tabla.Columns.Add("Id", typeof(Guid));
        tabla.Columns.Add("FechaEmision", typeof(DateTime));
        tabla.Columns.Add("NumeroOrden", typeof(string));
        tabla.Columns.Add("UserId", typeof(int));
        tabla.Columns.Add("ProveedorNit", typeof(string));
        tabla.Columns.Add("ProveedorNombre", typeof(string));
        tabla.Columns.Add("Monto", typeof(decimal));
        tabla.Columns.Add("Moneda", typeof(string));
        tabla.Columns.Add("Estado", typeof(string));
        tabla.Columns.Add("BodegaDestino", typeof(string));
        tabla.Columns.Add("Detalle", typeof(string));
        tabla.Columns.Add("CreatedAt", typeof(DateTime));
        return tabla;
    }
}
