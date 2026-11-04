using Seminario.Application.Common;
using Seminario.Application.DTOs.Reportes;

namespace Seminario.Application.Interfaces;

/// <summary>
/// Reportes de la Fase 3. El calculo pesado (ranking, sumas, conteos, filtros y paginacion) lo hace
/// SQL Server; el backend no ordena ni agrega en memoria. Cada consulta deja un evento de auditoria.
/// </summary>
public interface IReporteService
{
    /// <summary>Ranking de proveedores del anio via dbo.FN_Ranking_Proveedores (RANK() en SQL Server).</summary>
    Task<List<RankingProveedorDto>> GetRankingProveedoresAsync(UsuarioSesion sesion, int? anio, int? top);

    /// <summary>Reporte transaccional de ordenes. El rango de fechas es obligatorio (particion).</summary>
    Task<ReporteOrdenesResultDto> GetOrdenesAsync(UsuarioSesion sesion, ReporteOrdenesQueryDto query);

    /// <summary>Registros con soft delete y traza de auditoria (ordenes, lineas o documentos). Solo Administrador.</summary>
    Task<AuditoriaSoftDeleteResultDto> GetAuditoriaSoftDeleteAsync(UsuarioSesion sesion, AuditoriaSoftDeleteQueryDto query);
}
