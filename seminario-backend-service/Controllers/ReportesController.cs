using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seminario.Api;
using Seminario.Application.DTOs.Reportes;
using Seminario.Application.Interfaces;

namespace Seminario.Controllers;

/// <summary>
/// Reportes de la Fase 3 (todo el calculo lo hace SQL Server):
///  - NivelEstandar (USER, INTERMEDIO, ADMIN): ranking de proveedores y reporte transaccional de ordenes.
///  - NivelAdministrador (ADMIN): reporte de auditoria de soft delete (ordenes, lineas y documentos).
/// </summary>
[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    /// <summary>
    /// Ranking de proveedores por total comprometido del anio (RANK() en SQL Server).
    /// anio 2024..2027 (default: el actual), top 1..100 (default 20); fuera de rango responde 422.
    /// </summary>
    [HttpGet("ranking-proveedores")]
    [Authorize(Policy = AuthorizationPolicies.NivelEstandar)]
    public async Task<ActionResult<List<RankingProveedorDto>>> RankingProveedores([FromQuery] int? anio, [FromQuery] int? top)
    {
        return Ok(await _reporteService.GetRankingProveedoresAsync(User.ToUsuarioSesion(), anio, top));
    }

    /// <summary>
    /// Reporte transaccional de ordenes. fechaDesde y fechaHasta son obligatorias (columna de particion):
    /// sin ellas responde 400. Rango maximo 366 dias.
    /// </summary>
    [HttpGet("ordenes")]
    [Authorize(Policy = AuthorizationPolicies.NivelEstandar)]
    public async Task<ActionResult<ReporteOrdenesResultDto>> Ordenes([FromQuery] ReporteOrdenesQueryDto query)
    {
        return Ok(await _reporteService.GetOrdenesAsync(User.ToUsuarioSesion(), query));
    }

    /// <summary>
    /// Registros con soft delete y traza de auditoria. tipo = ordenes (default), lineas o documentos.
    /// Solo Administrador: Estandar e Intermedio reciben 403.
    /// </summary>
    [HttpGet("auditoria/soft-delete")]
    [Authorize(Policy = AuthorizationPolicies.NivelAdministrador)]
    public async Task<ActionResult<AuditoriaSoftDeleteResultDto>> AuditoriaSoftDelete([FromQuery] AuditoriaSoftDeleteQueryDto query)
    {
        return Ok(await _reporteService.GetAuditoriaSoftDeleteAsync(User.ToUsuarioSesion(), query));
    }
}
