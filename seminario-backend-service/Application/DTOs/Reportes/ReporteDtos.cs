using Seminario.Application.Common;

namespace Seminario.Application.DTOs.Reportes;

/// <summary>Fila del ranking que devuelve dbo.FN_Ranking_Proveedores (ya numerada por RANK() en SQL Server).</summary>
public class RankingProveedorDto
{
    public int PosicionRanking { get; set; }
    public string ProveedorNit { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;

    /// <summary>Suma del Importe de las lineas vigentes de ordenes no anuladas del anio.</summary>
    public decimal TotalComprometido { get; set; }
    public int Ordenes { get; set; }
    public int Lineas { get; set; }

    /// <summary>Porcentaje sobre el total comprometido del anio (todos los proveedores).</summary>
    public decimal ParticipacionPct { get; set; }
}

/// <summary>
/// Filtros del reporte transaccional. FechaDesde y FechaHasta son OBLIGATORIAS (columna de particion):
/// sin ellas SQL Server recorreria todas las particiones. Se validan en el servicio para responder 400.
/// </summary>
public class ReporteOrdenesQueryDto
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? ProveedorNit { get; set; }
    public string? Estado { get; set; }
    public string? Bodega { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ReporteOrdenItemDto
{
    public Guid Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string ProveedorNit { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;
    public string BodegaDestino { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int LineasVigentes { get; set; }
    public int LineasAnuladas { get; set; }
}

public class ReporteOrdenesResultDto : PagedResult<ReporteOrdenItemDto>
{
    /// <summary>SUM(Monto) de TODAS las filas que cumplen los filtros, no solo de la pagina.</summary>
    public decimal MontoTotal { get; set; }
}

/// <summary>Filtros del reporte de auditoria de soft delete (fechas sobre la fecha de la baja).</summary>
public class AuditoriaSoftDeleteQueryDto
{
    /// <summary>ordenes (default), lineas o documentos.</summary>
    public string? Tipo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditoriaSoftDeleteItemDto
{
    /// <summary>ORDEN, LINEA o DOCUMENTO.</summary>
    public string Tipo { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public decimal? Monto { get; set; }
    public DateTime FechaBaja { get; set; }
    public int? UsuarioBajaId { get; set; }
    public string? UsuarioBaja { get; set; }
    public string? Motivo { get; set; }

    /// <summary>Id de la orden (guid) para ORDEN y LINEA; Id del documento para DOCUMENTO.</summary>
    public string EntidadId { get; set; } = string.Empty;
}

public class AuditoriaResumenDto
{
    public int Ordenes { get; set; }
    public int Lineas { get; set; }
    public int Documentos { get; set; }
}

public class AuditoriaSoftDeleteResultDto : PagedResult<AuditoriaSoftDeleteItemDto>
{
    /// <summary>Conteo de cada tipo (con el mismo rango de fechas), sin importar el tipo pedido ni la pagina.</summary>
    public AuditoriaResumenDto Resumen { get; set; } = new();
}
