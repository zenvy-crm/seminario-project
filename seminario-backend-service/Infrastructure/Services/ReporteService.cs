using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Reportes;
using Seminario.Application.Interfaces;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Services;

/// <summary>
/// Reportes de la Fase 3. Todo el trabajo pesado se hace en SQL Server:
///  - Ranking: dbo.FN_Ranking_Proveedores (RANK() OVER) consultada con SqlQuery parametrizado.
///  - Ordenes: LINQ con AsNoTracking; filtros, SUM, COUNT y paginacion se traducen a SQL. El rango
///    de fechas (columna de particion) es obligatorio para que SQL Server elimine particiones.
///  - Auditoria: solo filas con soft delete Y traza (quien / cuando), unidas con Users.
/// </summary>
public class ReporteService : IReporteService
{
    public const string ModuloAuditoria = "REPORTES";

    private const int AnioMinimo = 2024;
    private const int AnioMaximo = 2027;
    private const int TopPorDefecto = 20;
    private const int TopMaximo = 100;
    private const int PageSizePorDefecto = 50;
    private const int PageSizeMaximo = 200;
    private const int DiasMaximosRango = 366;

    private const string TipoOrdenes = "ordenes";
    private const string TipoLineas = "lineas";
    private const string TipoDocumentos = "documentos";

    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public ReporteService(AppDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<List<RankingProveedorDto>> GetRankingProveedoresAsync(UsuarioSesion sesion, int? anio, int? top)
    {
        var anioConsulta = anio ?? DateTime.UtcNow.Year;
        var topConsulta = top ?? TopPorDefecto;

        var errores = new List<string>();
        if (anioConsulta is < AnioMinimo or > AnioMaximo)
        {
            errores.Add($"El anio debe estar entre {AnioMinimo} y {AnioMaximo}");
        }
        if (topConsulta is < 1 or > TopMaximo)
        {
            errores.Add($"top debe estar entre 1 y {TopMaximo}");
        }
        if (errores.Count > 0)
        {
            throw new ValidationApiException("Datos invalidos", errores);
        }

        // El RANK lo calcula SQL Server. Los valores viajan como parametros (@p0 / @p1), nunca
        // concatenados; el OrderBy de LINQ se traduce a SQL, no ordena en memoria.
        var anioSql = (short)anioConsulta;
        var filas = await _db.Database
            .SqlQuery<RankingRow>($"SELECT TOP ({topConsulta}) * FROM dbo.FN_Ranking_Proveedores({anioSql}) ORDER BY PosicionRanking, ProveedorNit")
            .OrderBy(r => r.PosicionRanking)
            .ThenBy(r => r.ProveedorNit)
            .ToListAsync();

        var resultado = filas.Select(r => new RankingProveedorDto
        {
            PosicionRanking = r.PosicionRanking,
            ProveedorNit = r.ProveedorNit,
            ProveedorNombre = r.ProveedorNombre,
            TotalComprometido = r.TotalComprometido,
            Ordenes = r.Ordenes,
            Lineas = r.Lineas,
            ParticipacionPct = r.ParticipacionPct
        }).ToList();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.ReporteConsultado,
            Entity = ModuloAuditoria,
            EntityId = "RANKING_PROVEEDORES",
            ActorUserId = sesion.UserId,
            ActorUsername = sesion.Username,
            After = new { reporte = "ranking-proveedores", anio = anioConsulta, top = topConsulta, filas = resultado.Count }
        });

        return resultado;
    }

    public async Task<ReporteOrdenesResultDto> GetOrdenesAsync(UsuarioSesion sesion, ReporteOrdenesQueryDto query)
    {
        if (query.FechaDesde is null || query.FechaHasta is null)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Debe indicar el rango de fechas (desde y hasta) para ejecutar el reporte.");
        }

        var desdeDia = query.FechaDesde.Value.Date;
        var hastaDia = query.FechaHasta.Value.Date;
        if (desdeDia > hastaDia)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "La fecha desde no puede ser mayor que la fecha hasta.");
        }
        if ((hastaDia - desdeDia).Days + 1 > DiasMaximosRango)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                $"El rango de fechas no puede exceder {DiasMaximosRango} dias. Acorte el rango para no recorrer toda la tabla.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > PageSizeMaximo ? PageSizePorDefecto : query.PageSize;

        // Rango semiabierto [desde, hasta + 1 dia): permite la eliminacion de particiones.
        var desde = DateTime.SpecifyKind(desdeDia, DateTimeKind.Unspecified);
        var hastaExclusivo = DateTime.SpecifyKind(hastaDia.AddDays(1), DateTimeKind.Unspecified);

        var q = _db.OrdenesCompra.AsNoTracking()
            .Where(o => o.FechaEmision >= desde && o.FechaEmision < hastaExclusivo);

        // Igual que en Operaciones: el rol Estandar nunca ve ordenes ANULADA (soft delete).
        if (sesion.EsEstandar)
        {
            q = q.Where(o => o.Estado != OrdenEstado.Anulada);
        }

        var nit = query.ProveedorNit?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(nit))
        {
            q = q.Where(o => o.ProveedorNit == nit);
        }

        var estado = query.Estado?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(estado))
        {
            q = q.Where(o => o.Estado == estado);
        }

        var bodega = query.Bodega?.Trim();
        if (!string.IsNullOrEmpty(bodega))
        {
            q = q.Where(o => o.BodegaDestino == bodega);
        }

        var total = await q.CountAsync();
        var montoTotal = await q.SumAsync(o => (decimal?)o.Monto) ?? 0m;

        var items = await q
            .OrderByDescending(o => o.FechaEmision)
            .ThenBy(o => o.NumeroOrden)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new ReporteOrdenItemDto
            {
                Id = o.Id,
                NumeroOrden = o.NumeroOrden,
                FechaEmision = o.FechaEmision,
                ProveedorNit = o.ProveedorNit,
                ProveedorNombre = o.ProveedorNombre,
                BodegaDestino = o.BodegaDestino,
                Estado = o.Estado,
                Moneda = o.Moneda,
                Monto = o.Monto,
                LineasVigentes = o.Lineas.Count(l => l.EstadoLinea),
                LineasAnuladas = o.Lineas.Count(l => !l.EstadoLinea)
            })
            .ToListAsync();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.ReporteConsultado,
            Entity = ModuloAuditoria,
            EntityId = "ORDENES",
            ActorUserId = sesion.UserId,
            ActorUsername = sesion.Username,
            After = new
            {
                reporte = "ordenes",
                fechaDesde = desdeDia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                fechaHasta = hastaDia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                proveedorNit = nit,
                estado,
                bodega,
                page,
                pageSize,
                total
            }
        });

        return new ReporteOrdenesResultDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            MontoTotal = montoTotal
        };
    }

    public async Task<AuditoriaSoftDeleteResultDto> GetAuditoriaSoftDeleteAsync(UsuarioSesion sesion, AuditoriaSoftDeleteQueryDto query)
    {
        var tipo = string.IsNullOrWhiteSpace(query.Tipo) ? TipoOrdenes : query.Tipo.Trim().ToLowerInvariant();
        if (tipo is not (TipoOrdenes or TipoLineas or TipoDocumentos))
        {
            throw new ValidationApiException("Datos invalidos",
                ["tipo debe ser ordenes, lineas o documentos"]);
        }

        DateTime? desde = null;
        DateTime? hastaExclusivo = null;
        if (query.FechaDesde is { } fd)
        {
            desde = DateTime.SpecifyKind(fd.Date, DateTimeKind.Unspecified);
        }
        if (query.FechaHasta is { } fh)
        {
            hastaExclusivo = DateTime.SpecifyKind(fh.Date.AddDays(1), DateTimeKind.Unspecified);
        }
        if (desde.HasValue && hastaExclusivo.HasValue && desde >= hastaExclusivo)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "La fecha desde no puede ser mayor que la fecha hasta.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > PageSizeMaximo ? PageSizePorDefecto : query.PageSize;

        // Solo entran filas con soft delete Y traza de auditoria: las ~12k ANULADA del stress test
        // (sin FechaAnulacion) no cuentan como bajas auditadas.
        var ordenes = _db.OrdenesCompra.AsNoTracking()
            .Where(o => o.Estado == OrdenEstado.Anulada && o.FechaAnulacion != null);
        var lineas = _db.OrdenesCompraDetalle.AsNoTracking()
            .Where(l => !l.EstadoLinea && l.FechaBaja != null);
        var documentos = _db.UserDocuments.IgnoreQueryFilters().AsNoTracking()
            .Where(d => d.IsDeleted);

        if (desde.HasValue)
        {
            ordenes = ordenes.Where(o => o.FechaAnulacion >= desde);
            lineas = lineas.Where(l => l.FechaBaja >= desde);
            documentos = documentos.Where(d => d.DeletedAt >= desde);
        }
        if (hastaExclusivo.HasValue)
        {
            ordenes = ordenes.Where(o => o.FechaAnulacion < hastaExclusivo);
            lineas = lineas.Where(l => l.FechaBaja < hastaExclusivo);
            documentos = documentos.Where(d => d.DeletedAt < hastaExclusivo);
        }

        var resumen = new AuditoriaResumenDto
        {
            Ordenes = await ordenes.CountAsync(),
            Lineas = await lineas.CountAsync(),
            Documentos = await documentos.CountAsync()
        };

        var total = tipo switch
        {
            TipoLineas => resumen.Lineas,
            TipoDocumentos => resumen.Documentos,
            _ => resumen.Ordenes
        };

        // Paginacion y orden en SQL; el username de quien dio de baja sale de una subconsulta a Users.
        List<AuditoriaFila> filas;
        switch (tipo)
        {
            case TipoLineas:
                filas = await lineas
                    .OrderByDescending(l => l.FechaBaja).ThenBy(l => l.Orden!.NumeroOrden).ThenBy(l => l.Linea)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(l => new AuditoriaFila
                    {
                        Tipo = "LINEA",
                        Referencia = l.Orden!.NumeroOrden,
                        OrdenId = l.OrdenId,
                        Linea = l.Linea,
                        Texto = l.Descripcion,
                        Monto = l.Importe,
                        FechaBaja = l.FechaBaja!.Value,
                        UsuarioBajaId = l.UsuarioBajaId,
                        UsuarioBaja = _db.Users.Where(u => u.Id == l.UsuarioBajaId).Select(u => u.Username).FirstOrDefault(),
                        Motivo = l.MotivoBaja
                    })
                    .ToListAsync();
                break;

            case TipoDocumentos:
                filas = await documentos
                    .OrderByDescending(d => d.DeletedAt).ThenBy(d => d.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(d => new AuditoriaFila
                    {
                        Tipo = "DOCUMENTO",
                        DocumentoId = d.Id,
                        Texto = d.FileName,
                        FechaBaja = d.DeletedAt ?? d.CreatedAt,
                        UsuarioBajaId = d.DeletedByUserId,
                        UsuarioBaja = _db.Users.Where(u => u.Id == d.DeletedByUserId).Select(u => u.Username).FirstOrDefault()
                    })
                    .ToListAsync();
                break;

            default:
                filas = await ordenes
                    .OrderByDescending(o => o.FechaAnulacion).ThenBy(o => o.NumeroOrden)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(o => new AuditoriaFila
                    {
                        Tipo = "ORDEN",
                        Referencia = o.NumeroOrden,
                        OrdenId = o.Id,
                        Texto = o.ProveedorNombre,
                        Monto = o.Monto,
                        FechaBaja = o.FechaAnulacion!.Value,
                        UsuarioBajaId = o.UsuarioAnulaId,
                        UsuarioBaja = _db.Users.Where(u => u.Id == o.UsuarioAnulaId).Select(u => u.Username).FirstOrDefault(),
                        Motivo = o.MotivoAnulacion
                    })
                    .ToListAsync();
                break;
        }

        var items = filas.Select(ToItem).ToList();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.ReporteAuditoriaConsultado,
            Entity = ModuloAuditoria,
            EntityId = "AUDITORIA_SOFT_DELETE",
            ActorUserId = sesion.UserId,
            ActorUsername = sesion.Username,
            After = new
            {
                reporte = "auditoria-soft-delete",
                tipo,
                fechaDesde = query.FechaDesde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                fechaHasta = query.FechaHasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                page,
                pageSize,
                total
            }
        });

        return new AuditoriaSoftDeleteResultDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            Resumen = resumen
        };
    }

    private static AuditoriaSoftDeleteItemDto ToItem(AuditoriaFila f)
    {
        var esDocumento = f.Tipo == "DOCUMENTO";
        return new AuditoriaSoftDeleteItemDto
        {
            Tipo = f.Tipo,
            Referencia = esDocumento ? $"DOC-{f.DocumentoId}" : f.Referencia,
            Detalle = f.Tipo == "LINEA" ? $"Linea {f.Linea}: {f.Texto}" : f.Texto,
            Monto = f.Monto,
            FechaBaja = f.FechaBaja,
            UsuarioBajaId = f.UsuarioBajaId,
            UsuarioBaja = f.UsuarioBaja,
            Motivo = f.Motivo,
            EntidadId = esDocumento
                ? f.DocumentoId.ToString(CultureInfo.InvariantCulture)
                : f.OrdenId.ToString()
        };
    }

    /// <summary>Fila comun de las tres consultas de auditoria (se arma en SQL y se traduce al DTO en memoria).</summary>
    private sealed class AuditoriaFila
    {
        public string Tipo { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public Guid OrdenId { get; set; }
        public int DocumentoId { get; set; }
        public int Linea { get; set; }
        public string? Texto { get; set; }
        public decimal? Monto { get; set; }
        public DateTime FechaBaja { get; set; }
        public int? UsuarioBajaId { get; set; }
        public string? UsuarioBaja { get; set; }
        public string? Motivo { get; set; }
    }

    /// <summary>Fila que devuelve dbo.FN_Ranking_Proveedores.</summary>
    private sealed class RankingRow
    {
        public int PosicionRanking { get; set; }
        public string ProveedorNit { get; set; } = string.Empty;
        public string ProveedorNombre { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal TotalComprometido { get; set; }
        public int Ordenes { get; set; }
        public int Lineas { get; set; }

        [Precision(5, 2)]
        public decimal ParticipacionPct { get; set; }
    }
}
