using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Seminario.Api;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Operaciones;
using Seminario.Application.Interfaces;

namespace Seminario.Controllers;

/// <summary>
/// Modulo Orden de Compra. RBAC de la Fase 2 con claims del JWT:
///  - NivelEstandar (USER, INTERMEDIO, ADMIN): crear (nace CREADA) y consultar.
///  - NivelIntermedio (INTERMEDIO, ADMIN): PUT sobre una linea = soft delete (EstadoLinea = 0).
///  - NivelAdministrador (ADMIN): DELETE de la orden = soft delete (Estado = ANULADA).
/// Nunca se ejecuta un DELETE fisico.
/// </summary>
[ApiController]
[Route("api/operaciones")]
[Authorize]
public class OperacionesController : ControllerBase
{
    private readonly IOrdenCompraService _ordenCompraService;

    public OperacionesController(IOrdenCompraService ordenCompraService)
    {
        _ordenCompraService = ordenCompraService;
    }

    /// <summary>
    /// Registra una orden de compra con sus lineas via SP_OrdenCompra_Crear (nace CREADA).
    /// </summary>
    [HttpPost("ordenes")]
    [Authorize(Policy = AuthorizationPolicies.NivelEstandar)]
    public async Task<ActionResult<OrdenCompraDto>> CrearOrden([FromBody] CreateOrdenCompraDto dto)
    {
        var orden = await _ordenCompraService.CreateAsync(User.ToUsuarioSesion(), dto);
        return CreatedAtAction(nameof(ObtenerOrden),
            new { id = orden.Id, fechaEmision = orden.FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }, orden);
    }

    /// <summary>Listado paginado. El rol Estandar nunca ve ordenes ANULADA.</summary>
    [HttpGet("ordenes")]
    [Authorize(Policy = AuthorizationPolicies.NivelEstandar)]
    public async Task<ActionResult<PagedResult<OrdenCompraDto>>> ListarOrdenes([FromQuery] OrdenCompraQueryDto query)
    {
        return Ok(await _ordenCompraService.GetPagedAsync(User.ToUsuarioSesion(), query));
    }

    /// <summary>Encabezado + lineas. fechaEmision (yyyy-MM-dd) es opcional y habilita la eliminacion de particiones.</summary>
    [HttpGet("ordenes/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.NivelEstandar)]
    public async Task<ActionResult<OrdenCompraDto>> ObtenerOrden(Guid id, [FromQuery] DateTime? fechaEmision)
    {
        return Ok(await _ordenCompraService.GetByIdAsync(User.ToUsuarioSesion(), id, fechaEmision));
    }

    /// <summary>Presupuesto de compras por bodega y anio (la "capacidad" que consumen las lineas).</summary>
    [HttpGet("presupuestos")]
    [Authorize(Policy = AuthorizationPolicies.NivelEstandar)]
    public async Task<ActionResult<List<PresupuestoBodegaDto>>> Presupuestos([FromQuery] int? anio)
    {
        return Ok(await _ordenCompraService.GetPresupuestosAsync(anio));
    }

    /// <summary>
    /// Soft delete de una linea del detalle (EstadoLinea = 0). El trigger libera su importe del presupuesto.
    /// </summary>
    [HttpPut("ordenes/{id:guid}/lineas/{linea:int}")]
    [Authorize(Policy = AuthorizationPolicies.NivelIntermedio)]
    public async Task<ActionResult<OrdenCompraDto>> AnularLinea(
        Guid id,
        int linea,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AnularLineaDto? dto,
        [FromQuery] DateTime? fechaEmision,
        [FromQuery] string? motivo)
    {
        dto ??= new AnularLineaDto();
        if (string.IsNullOrWhiteSpace(dto.Motivo))
        {
            dto.Motivo = motivo;
        }

        return Ok(await _ordenCompraService.AnularLineaAsync(User.ToUsuarioSesion(), id, linea, fechaEmision, dto));
    }

    /// <summary>
    /// "Eliminar" una orden: en realidad es un Soft Delete (Estado = ANULADA). La fila sigue en la base.
    /// La justificacion va en el cuerpo {"motivo": "..."} o, si el cliente no manda cuerpo, en ?motivo=.
    /// </summary>
    [HttpDelete("ordenes/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.NivelAdministrador)]
    public async Task<ActionResult<OrdenCompraDto>> AnularOrden(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AnularOrdenDto? dto,
        [FromQuery] DateTime? fechaEmision,
        [FromQuery] string? motivo)
    {
        dto ??= new AnularOrdenDto();
        if (string.IsNullOrWhiteSpace(dto.Motivo))
        {
            dto.Motivo = motivo;
        }

        return Ok(await _ordenCompraService.AnularOrdenAsync(User.ToUsuarioSesion(), id, fechaEmision, dto));
    }

    [HttpGet("particiones")]
    public async Task<ActionResult<List<ParticionInfoDto>>> Particiones()
    {
        return Ok(await _ordenCompraService.GetParticionesAsync());
    }
}
