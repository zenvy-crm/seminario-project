using Seminario.Application.Common;
using Seminario.Application.DTOs.Operaciones;

namespace Seminario.Application.Interfaces;

/// <summary>
/// Modulo Orden de Compra (Fase 2). Ninguna operacion hace DELETE fisico: las bajas son
/// soft delete (EstadoLinea = 0 en el detalle, Estado = ANULADA en el encabezado) y los
/// triggers de SQL Server ajustan el presupuesto de la bodega.
/// La sesion (usuario, rol y nivel) la arma el controlador a partir del JWT.
/// </summary>
public interface IOrdenCompraService
{
    /// <summary>Crea la orden (estado CREADA) y sus lineas en una sola transaccion via SP_OrdenCompra_Crear.</summary>
    Task<OrdenCompraDto> CreateAsync(UsuarioSesion sesion, CreateOrdenCompraDto dto);

    /// <summary>Listado paginado. El rol Estandar nunca ve ordenes ANULADA.</summary>
    Task<PagedResult<OrdenCompraDto>> GetPagedAsync(UsuarioSesion sesion, OrdenCompraQueryDto query);

    /// <summary>Encabezado + lineas (incluidas las anuladas). Para el rol Estandar una ANULADA es 404.</summary>
    /// <param name="fechaEmision">Opcional: habilita la eliminacion de particiones al buscar por Id.</param>
    Task<OrdenCompraDto> GetByIdAsync(UsuarioSesion sesion, Guid id, DateTime? fechaEmision);

    Task<List<PresupuestoBodegaDto>> GetPresupuestosAsync(int? anio);

    /// <summary>Soft delete de una linea (EstadoLinea = 0). El trigger devuelve su importe al presupuesto.</summary>
    Task<OrdenCompraDto> AnularLineaAsync(UsuarioSesion sesion, Guid id, int linea, DateTime? fechaEmision, AnularLineaDto dto);

    /// <summary>Soft delete de la orden (Estado = ANULADA). El trigger libera lo comprometido.</summary>
    Task<OrdenCompraDto> AnularOrdenAsync(UsuarioSesion sesion, Guid id, DateTime? fechaEmision, AnularOrdenDto dto);

    /// <summary>Auditoria fisica: cuantas filas quedaron en cada particion.</summary>
    Task<List<ParticionInfoDto>> GetParticionesAsync();
}

public interface ISeedDataService
{
    Task<SeedDataResultDto> InyectarAsync(int userId, SeedDataRequestDto request, CancellationToken cancellationToken);
}
