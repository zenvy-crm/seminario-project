using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Operaciones;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Services;

/// <summary>
/// Modulo Orden de Compra de la Fase 2. La logica transaccional vive en SQL Server:
///  - Alta: SP_OrdenCompra_Crear (encabezado + N lineas en una transaccion, rollback absoluto).
///  - Presupuesto: los triggers TR_OC_Detalle_Presupuesto / TR_OC_Encabezado_Anulacion lo comprometen
///    y lo liberan; este servicio NUNCA calcula Monto ni MontoComprometido, solo los vuelve a leer.
///  - Bajas: siempre soft delete (UPDATE). Un DELETE fisico lo rechazan los triggers INSTEAD OF DELETE.
/// </summary>
public class OrdenCompraService : IOrdenCompraService
{
    /// <summary>Descripcion de la linea unica que se crea cuando el formulario solo manda un Monto.</summary>
    public const string DescripcionMontoGlobal = "Monto global";

    /// <summary>Valor del campo "modulo" de la auditoria en MongoDB para todo el modulo.</summary>
    public const string ModuloAuditoria = "ORDEN_COMPRA";

    // El SP lee las llaves en camelCase y es sensible a mayusculas.
    private static readonly JsonSerializerOptions SpJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] EstadosModificables = { OrdenEstado.Creada, OrdenEstado.Emitida };

    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public OrdenCompraService(AppDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<OrdenCompraDto> CreateAsync(UsuarioSesion sesion, CreateOrdenCompraDto dto)
    {
        var username = await _db.Users.AsNoTracking()
            .Where(u => u.Id == sesion.UserId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("El usuario de la sesion ya no existe");

        var hoy = DateTime.UtcNow.Date;
        var fecha = (dto.FechaEmision ?? hoy).Date;
        if (fecha > hoy.AddDays(1))
        {
            throw new ValidationApiException("Datos invalidos",
                ["La fecha de emision no puede ser futura"]);
        }

        // Compatibilidad con el formulario actual (solo Monto): se convierte en UNA linea.
        var lineas = dto.Lineas ?? new List<CreateLineaOrdenDto>();

        // La validacion de modelo no revisa elementos null dentro de la lista ("lineas":[null]).
        if (lineas.Any(l => l is null))
        {
            throw new ValidationApiException("Datos invalidos",
                ["Cada elemento de 'lineas' debe ser un objeto con descripcion, cantidad y precioUnitario"]);
        }
        if (lineas.Count == 0)
        {
            if (dto.Monto is not > 0)
            {
                throw new ValidationApiException("Datos invalidos",
                    ["La orden debe traer al menos una linea en 'lineas' o un Monto mayor a cero"]);
            }

            lineas = new List<CreateLineaOrdenDto>
            {
                new() { Descripcion = DescripcionMontoGlobal, Cantidad = 1, PrecioUnitario = dto.Monto.Value }
            };
        }

        var json = JsonSerializer.Serialize(new
        {
            FechaEmision = fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ProveedorNit = dto.ProveedorNit.Trim().ToUpperInvariant(),
            ProveedorNombre = dto.ProveedorNombre.Trim(),
            Moneda = dto.Moneda.Trim().ToUpperInvariant(),
            BodegaDestino = dto.BodegaDestino.Trim(),
            Detalle = string.IsNullOrWhiteSpace(dto.Detalle) ? null : dto.Detalle.Trim(),
            Lineas = lineas.Select(l => new
            {
                Descripcion = l.Descripcion.Trim(),
                l.Cantidad,
                l.PrecioUnitario
            })
        }, SpJsonOptions);

        // Sin transaccion externa: el SP abre la suya (BEGIN TRAN / COMMIT / ROLLBACK absoluto).
        List<SpCrearResultado> resultado;
        try
        {
            resultado = await _db.Database
                .SqlQueryRaw<SpCrearResultado>(
                    $"EXEC [dbo].[{OrdenCompraSqlObjects.SpCrear}] @Json, @UserId",
                    new SqlParameter("@Json", SqlDbType.NVarChar, -1) { Value = json },
                    new SqlParameter("@UserId", SqlDbType.Int) { Value = sesion.UserId })
                .ToListAsync();
        }
        catch (SqlException ex) when (TraducirErrorSql(ex) is { } apiEx)
        {
            throw apiEx;
        }

        var creada = resultado.FirstOrDefault()
            ?? throw new InvalidOperationException("SP_OrdenCompra_Crear no devolvio la orden creada");

        var orden = await LeerOrdenAsync(creada.Id, creada.FechaEmision, incluirPresupuesto: true)
            ?? throw new InvalidOperationException("La orden recien creada no se encontro");

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.OrdenCompraCreada,
            Entity = ModuloAuditoria,
            EntityId = orden.NumeroOrden,
            UserId = sesion.UserId,
            Username = username,
            After = new
            {
                id = orden.Id,
                numeroOrden = orden.NumeroOrden,
                fechaEmision = orden.FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                proveedorNit = orden.ProveedorNit,
                proveedorNombre = orden.ProveedorNombre,
                monto = orden.Monto,
                moneda = orden.Moneda,
                estado = orden.Estado,
                bodegaDestino = orden.BodegaDestino,
                lineas = orden.Lineas.Count,
                comprometido = orden.Presupuesto?.MontoComprometido
            }
        });

        return orden;
    }

    public async Task<PagedResult<OrdenCompraDto>> GetPagedAsync(UsuarioSesion sesion, OrdenCompraQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var q = _db.OrdenesCompra.AsNoTracking().AsQueryable();

        // RBAC: el soft delete oculta la orden al rol Estandar, aunque pida Estado=ANULADA.
        if (sesion.EsEstandar)
        {
            q = q.Where(o => o.Estado != OrdenEstado.Anulada);
        }

        // El filtro por rango de fechas es lo que permite a SQL Server descartar
        // particiones completas (partition elimination) en lugar de recorrer la tabla.
        if (query.Desde.HasValue)
        {
            var desde = DateTime.SpecifyKind(query.Desde.Value.Date, DateTimeKind.Unspecified);
            q = q.Where(o => o.FechaEmision >= desde);
        }

        if (query.Hasta.HasValue)
        {
            var hasta = DateTime.SpecifyKind(query.Hasta.Value.Date.AddDays(1), DateTimeKind.Unspecified);
            q = q.Where(o => o.FechaEmision < hasta);
        }

        if (!string.IsNullOrWhiteSpace(query.Estado))
        {
            var estado = query.Estado.Trim().ToUpperInvariant();
            q = q.Where(o => o.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(query.ProveedorNit))
        {
            var nit = query.ProveedorNit.Trim().ToUpperInvariant();
            q = q.Where(o => o.ProveedorNit == nit);
        }

        if (query.SoloMias == true)
        {
            q = q.Where(o => o.UserId == sesion.UserId);
        }

        if (query.ConDetalle == true)
        {
            q = q.Where(o => o.Lineas.Any());
        }

        if (!string.IsNullOrWhiteSpace(query.NumeroOrden))
        {
            var numero = query.NumeroOrden.Trim().ToUpperInvariant();
            q = q.Where(o => o.NumeroOrden == numero);
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(o => o.FechaEmision)
            .ThenByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrdenCompraDto>
        {
            Items = items.Select(o => ToDto(o, null)).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrdenCompraDto> GetByIdAsync(UsuarioSesion sesion, Guid id, DateTime? fechaEmision)
    {
        var orden = await LeerOrdenAsync(id, fechaEmision, incluirPresupuesto: true);

        // Para el rol Estandar una orden anulada "no existe" (soft delete).
        if (orden is null || (sesion.EsEstandar && orden.Estado == OrdenEstado.Anulada))
        {
            throw new NotFoundException("La orden de compra no existe");
        }

        return orden;
    }

    public async Task<List<PresupuestoBodegaDto>> GetPresupuestosAsync(int? anio)
    {
        var q = _db.PresupuestosBodega.AsNoTracking().AsQueryable();
        if (anio.HasValue)
        {
            var filtro = (short)anio.Value;
            q = q.Where(p => p.Anio == filtro);
        }

        var filas = await q.OrderBy(p => p.Anio).ThenBy(p => p.Bodega).ToListAsync();
        return filas.Select(ToDto).ToList();
    }

    /// <summary>
    /// Soft delete de una linea: EstadoLinea = 0 + quien/cuando/por que. El trigger del detalle
    /// recalcula el Monto de la orden y devuelve el importe al presupuesto de la bodega.
    /// </summary>
    public async Task<OrdenCompraDto> AnularLineaAsync(
        UsuarioSesion sesion, Guid id, int linea, DateTime? fechaEmision, AnularLineaDto dto)
    {
        if (dto.EstadoLinea is { } estadoLinea && estadoLinea != 0)
        {
            throw new ValidationApiException("Datos invalidos",
                ["Solo se admite estadoLinea = 0 (anular la linea); una linea anulada no se reactiva"]);
        }

        var motivo = ValidarMotivo(dto.Motivo, "El motivo de la baja");

        var orden = await BuscarOrden(id, fechaEmision).AsNoTracking().FirstOrDefaultAsync()
            ?? throw new NotFoundException("La orden de compra no existe");

        ValidarEstadoModificable(orden, "anular lineas de");

        var entidad = await _db.OrdenesCompraDetalle
            .FirstOrDefaultAsync(l => l.FechaEmision == orden.FechaEmision && l.OrdenId == orden.Id && l.Linea == linea)
            ?? throw new NotFoundException($"La linea {linea} no existe en la orden {orden.NumeroOrden}");

        if (!entidad.EstadoLinea)
        {
            throw new ConflictException($"La linea {linea} de la orden {orden.NumeroOrden} ya estaba anulada");
        }

        const string mensajeConcurrencia = "La linea fue modificada por otro usuario. Recargue la orden e intente de nuevo.";
        AplicarRowVer(entidad, l => l.RowVer, dto.RowVer, mensajeConcurrencia);

        // Comprometido de la bodega antes de la baja, para la evidencia de auditoria.
        var anioPresupuesto = (short)orden.FechaEmision.Year;
        var comprometidoAnterior = await _db.PresupuestosBodega.AsNoTracking()
            .Where(p => p.Anio == anioPresupuesto && p.Bodega == orden.BodegaDestino)
            .Select(p => (decimal?)p.MontoComprometido)
            .FirstOrDefaultAsync();

        var importe = entidad.Importe;
        entidad.EstadoLinea = false;
        entidad.UsuarioBajaId = sesion.UserId;
        entidad.FechaBaja = AhoraSinFraccion();
        entidad.MotivoBaja = motivo;

        await GuardarAsync(mensajeConcurrencia);
        _db.ChangeTracker.Clear();

        // Monto (trigger) y presupuesto (trigger) se vuelven a leer de la base.
        var actualizada = await LeerOrdenAsync(orden.Id, orden.FechaEmision, incluirPresupuesto: true)
            ?? throw new NotFoundException("La orden de compra no existe");

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.LineaAnulada,
            Entity = ModuloAuditoria,
            EntityId = $"{orden.NumeroOrden}#{linea}",
            UserId = orden.UserId,
            ActorUserId = sesion.UserId,
            ActorUsername = sesion.Username,
            RolAuth = RoleName.RolAuthDe(sesion.Rol),
            Justificacion = entidad.MotivoBaja,
            Before = new { linea, estadoLinea = 1, importe, montoOrden = orden.Monto, comprometido = comprometidoAnterior },
            After = new
            {
                linea,
                estadoLinea = 0,
                montoOrden = actualizada.Monto,
                comprometido = actualizada.Presupuesto?.MontoComprometido,
                motivo = entidad.MotivoBaja
            }
        });

        return actualizada;
    }

    /// <summary>
    /// "DELETE" de la orden = soft delete: Estado = ANULADA + quien/cuando/por que. La fila sigue en
    /// la base (el rol Estandar deja de verla) y el trigger del encabezado libera lo comprometido.
    /// </summary>
    public async Task<OrdenCompraDto> AnularOrdenAsync(
        UsuarioSesion sesion, Guid id, DateTime? fechaEmision, AnularOrdenDto dto)
    {
        var motivo = ValidarMotivo(dto.Motivo, "La justificacion de la anulacion");

        var orden = await BuscarOrden(id, fechaEmision).FirstOrDefaultAsync()
            ?? throw new NotFoundException("La orden de compra no existe");

        ValidarEstadoModificable(orden, "anular");

        const string mensajeConcurrencia = "La orden fue modificada por otro usuario. Recargue la orden e intente de nuevo.";
        AplicarRowVer(orden, o => o.RowVer, dto.RowVer, mensajeConcurrencia);

        var estadoAnterior = orden.Estado;
        orden.Estado = OrdenEstado.Anulada;
        orden.UsuarioAnulaId = sesion.UserId;
        orden.FechaAnulacion = AhoraSinFraccion();
        orden.MotivoAnulacion = motivo;

        await GuardarAsync(mensajeConcurrencia);
        _db.ChangeTracker.Clear();

        // SQL Server ya quedo actualizado: la evidencia se manda a MongoDB en un hilo en segundo
        // plano (Task.Run dentro de FireAndForget) y la respuesta no espera a que termine.
        _auditLogger.FireAndForget(new AuditEvent
        {
            EventType = AuditEventTypes.AnulacionMaster,
            Entity = ModuloAuditoria,
            EntityId = orden.NumeroOrden,
            UserId = orden.UserId,
            ActorUserId = sesion.UserId,
            ActorUsername = sesion.Username,
            RolAuth = RoleName.RolAuthDe(sesion.Rol),
            Justificacion = orden.MotivoAnulacion,
            // Mismo calce que el documento de la consigna: solo el estado antes / despues. El monto,
            // la fecha y el usuario que anulo quedan en la fila de SQL Server (soft delete).
            Before = new { estado = estadoAnterior },
            After = new { estado = OrdenEstado.Anulada }
        });

        return await LeerOrdenAsync(orden.Id, orden.FechaEmision, incluirPresupuesto: true)
            ?? throw new NotFoundException("La orden de compra no existe");
    }

    /// <summary>
    /// PASO 5 del script: demuestra que SQL Server corto fisicamente la tabla.
    /// Se consulta con SQL nativo porque sys.partitions no forma parte del modelo de EF.
    /// </summary>
    public async Task<List<ParticionInfoDto>> GetParticionesAsync()
    {
        const string sql = @"
SELECT
    p.partition_number                                   AS NumeroParticion,
    fg.name                                              AS ArchivoFisico,
    ISNULL(CONVERT(varchar(10), rv_izq.value, 23), '-inf') AS RangoDesde,
    ISNULL(CONVERT(varchar(10), rv_der.value, 23), '+inf') AS RangoHasta,
    p.rows                                               AS RegistrosEnEsteFragmento
FROM sys.partitions p
JOIN sys.indexes i
    ON p.object_id = i.object_id AND p.index_id = i.index_id
JOIN sys.destination_data_spaces dds
    ON dds.partition_scheme_id = i.data_space_id AND dds.destination_id = p.partition_number
JOIN sys.filegroups fg
    ON dds.data_space_id = fg.data_space_id
JOIN sys.partition_schemes ps
    ON ps.data_space_id = i.data_space_id
JOIN sys.partition_functions pf
    ON pf.function_id = ps.function_id
LEFT JOIN sys.partition_range_values rv_izq
    ON rv_izq.function_id = pf.function_id AND rv_izq.boundary_id = p.partition_number - 1
LEFT JOIN sys.partition_range_values rv_der
    ON rv_der.function_id = pf.function_id AND rv_der.boundary_id = p.partition_number
WHERE p.object_id = OBJECT_ID('TB_Ordenes_Compra') AND i.index_id <= 1
ORDER BY p.partition_number;";

        var resultado = new List<ParticionInfoDto>();

        await using var connection = new SqlConnection(_db.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(new ParticionInfoDto
            {
                NumeroParticion = reader.GetInt32(0),
                ArchivoFisico = reader.GetString(1),
                RangoDesde = reader.GetString(2),
                RangoHasta = reader.GetString(3),
                RegistrosEnEsteFragmento = reader.GetInt64(4)
            });
        }

        return resultado;
    }

    /// <summary>
    /// Localiza una orden por Id. Con fechaEmision SQL Server solo toca una particion; sin ella
    /// usa el indice alineado IX_Ordenes_Id (un seek por particion).
    /// </summary>
    private IQueryable<OrdenCompra> BuscarOrden(Guid id, DateTime? fechaEmision)
    {
        var q = _db.OrdenesCompra.Where(o => o.Id == id);
        if (fechaEmision.HasValue)
        {
            var fecha = DateTime.SpecifyKind(fechaEmision.Value.Date, DateTimeKind.Unspecified);
            q = q.Where(o => o.FechaEmision == fecha);
        }

        return q;
    }

    /// <summary>Lee encabezado + lineas (y opcionalmente el presupuesto) tal como quedaron en la base.</summary>
    private async Task<OrdenCompraDto?> LeerOrdenAsync(Guid id, DateTime? fechaEmision, bool incluirPresupuesto)
    {
        var orden = await BuscarOrden(id, fechaEmision)
            .AsNoTracking()
            .Include(o => o.Lineas)
            .FirstOrDefaultAsync();

        if (orden is null)
        {
            return null;
        }

        PresupuestoBodega? presupuesto = null;
        if (incluirPresupuesto)
        {
            var anio = (short)orden.FechaEmision.Year;
            presupuesto = await _db.PresupuestosBodega.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Anio == anio && p.Bodega == orden.BodegaDestino);
        }

        return ToDto(orden, presupuesto);
    }

    /// <summary>
    /// El motivo se valida ya recortado: "  x  " no cuenta como justificacion de 3 caracteres.
    /// Es la evidencia que llega a "justificacion_cambio" en la auditoria.
    /// </summary>
    private static string ValidarMotivo(string? motivo, string campo)
    {
        var recortado = motivo?.Trim() ?? string.Empty;
        if (recortado.Length is < 3 or > 200)
        {
            throw new ValidationApiException("Datos invalidos",
                new List<string> { $"{campo} debe tener entre 3 y 200 caracteres sin contar espacios (en el cuerpo 'motivo' o en ?motivo=)" });
        }

        return recortado;
    }

    private static void ValidarEstadoModificable(OrdenCompra orden, string accion)
    {
        if (!EstadosModificables.Contains(orden.Estado))
        {
            throw new ConflictException(
                $"Solo se puede {accion} una orden en estado {string.Join(" o ", EstadosModificables)}; " +
                $"la orden {orden.NumeroOrden} esta {orden.Estado}");
        }
    }

    /// <summary>
    /// Bloqueo optimista explicito: si el cliente mando el rowVer que leyo, debe coincidir con el
    /// de la fila; ademas se fija como valor original para que el UPDATE lo verifique en la base.
    /// Si no lo mando, EF igual protege el intervalo entre la lectura y el SaveChanges.
    /// </summary>
    private void AplicarRowVer<T>(T entidad, Expression<Func<T, byte[]>> propiedad, string? rowVerBase64, string mensaje)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(rowVerBase64))
        {
            return;
        }

        byte[] esperado;
        try
        {
            esperado = Convert.FromBase64String(rowVerBase64.Trim());
        }
        catch (FormatException)
        {
            throw new ValidationApiException("Datos invalidos", ["rowVer no es un valor base64 valido"]);
        }

        var entry = _db.Entry(entidad).Property(propiedad);
        if (!entry.CurrentValue.SequenceEqual(esperado))
        {
            throw new ConflictException(mensaje);
        }

        entry.OriginalValue = esperado;
    }

    private async Task GuardarAsync(string mensajeConcurrencia)
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Otro proceso modifico la fila primero (rowversion no coincide).
            throw new ConflictException(mensajeConcurrencia, ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && TraducirErrorSql(sqlEx) is { } apiEx)
        {
            throw apiEx;
        }
    }

    /// <summary>
    /// Traduce los THROW de los SP/triggers (y los CHECK) a errores HTTP con mensaje util.
    /// Devuelve null si el error no es de negocio, para que el middleware lo trate como 500.
    /// </summary>
    private static ApiException? TraducirErrorSql(SqlException ex)
    {
        foreach (SqlError error in ex.Errors)
        {
            switch (error.Number)
            {
                case OrdenCompraSqlObjects.ErrorSinPresupuesto:
                    return new ValidationApiException("No existe presupuesto de compras para la bodega y el anio indicados",
                        [error.Message]);
                case OrdenCompraSqlObjects.ErrorLineaInvalida:
                    return new ValidationApiException("Datos de la orden invalidos. No se guardo la orden ni ninguna linea.",
                        [error.Message]);
                case 547 when error.Message.Contains(OrdenCompraSqlObjects.CheckComprometido, StringComparison.OrdinalIgnoreCase):
                    return new ConflictException("El presupuesto comprometido de la bodega quedaria negativo; la operacion se revirtio", ex);
                case 547:
                    return new ValidationApiException("Datos de la orden invalidos. No se guardo la orden ni ninguna linea.",
                        ["Una linea viola una restriccion de la base de datos (cantidad > 0, precio >= 0)"]);
                case 8115:
                    // Arithmetic overflow: un importe o total no cabe en decimal(18,2). El SP ya lo
                    // rechaza con 50011; esto cubre cualquier otro camino.
                    return new ValidationApiException("Datos de la orden invalidos. No se guardo la orden ni ninguna linea.",
                        ["Un importe o total excede el maximo permitido (9,999,999,999,999,999.99)"]);
                case 1205:
                    // Deadlock: SQL Server eligio esta transaccion como victima y la revirtio completa.
                    return new ConflictException("La orden estaba siendo modificada por otra operacion al mismo tiempo. Intente de nuevo.", ex);
                case OrdenCompraSqlObjects.ErrorPresupuestoInsuficiente:
                case OrdenCompraSqlObjects.ErrorOrdenAnulada:
                case OrdenCompraSqlObjects.ErrorBorradoFisico:
                case OrdenCompraSqlObjects.ErrorReversionAnulacion:
                case OrdenCompraSqlObjects.ErrorCambioBodega:
                    return new ConflictException(error.Message, ex);
            }
        }

        return null;
    }

    private static DateTime AhoraSinFraccion()
    {
        var ahora = DateTime.UtcNow;
        return new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, ahora.Second, DateTimeKind.Utc);
    }

    private static string ToBase64(byte[]? rowVer) =>
        rowVer is { Length: > 0 } ? Convert.ToBase64String(rowVer) : string.Empty;

    private static OrdenCompraDto ToDto(OrdenCompra o, PresupuestoBodega? presupuesto) => new()
    {
        Id = o.Id,
        NumeroOrden = o.NumeroOrden,
        FechaEmision = o.FechaEmision,
        UserId = o.UserId,
        ProveedorNit = o.ProveedorNit,
        ProveedorNombre = o.ProveedorNombre,
        Monto = o.Monto,
        Moneda = o.Moneda,
        Estado = o.Estado,
        BodegaDestino = o.BodegaDestino,
        Detalle = o.Detalle,
        CreatedAt = o.CreatedAt,
        UsuarioAnulaId = o.UsuarioAnulaId,
        FechaAnulacion = o.FechaAnulacion,
        MotivoAnulacion = o.MotivoAnulacion,
        RowVer = ToBase64(o.RowVer),
        Lineas = o.Lineas
            .OrderBy(l => l.Linea)
            .Select(l => new LineaOrdenDto
            {
                Linea = l.Linea,
                Descripcion = l.Descripcion,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                Importe = l.Importe,
                EstadoLinea = l.EstadoLinea,
                UsuarioBajaId = l.UsuarioBajaId,
                FechaBaja = l.FechaBaja,
                MotivoBaja = l.MotivoBaja,
                RowVer = ToBase64(l.RowVer)
            })
            .ToList(),
        Presupuesto = presupuesto is null ? null : ToDto(presupuesto)
    };

    private static PresupuestoBodegaDto ToDto(PresupuestoBodega p) => new()
    {
        Anio = p.Anio,
        Bodega = p.Bodega,
        MontoAsignado = p.MontoAsignado,
        MontoComprometido = p.MontoComprometido,
        MontoDisponible = p.MontoDisponible
    };

    /// <summary>Fila que devuelve SP_OrdenCompra_Crear despues del COMMIT.</summary>
    private sealed class SpCrearResultado
    {
        public Guid Id { get; set; }
        public DateTime FechaEmision { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal Monto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public byte[] RowVer { get; set; } = Array.Empty<byte>();
    }
}
