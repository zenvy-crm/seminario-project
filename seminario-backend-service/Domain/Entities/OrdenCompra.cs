namespace Seminario.Domain.Entities;

/// <summary>
/// Orden de compra emitida a un proveedor. Es la tabla de alta transaccionalidad del portal:
/// vive en SQL Server sobre un esquema de particion por <see cref="FechaEmision"/>.
/// Es el encabezado de <see cref="OrdenCompraDetalle"/>.
/// </summary>
public class OrdenCompra
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Columna de particion. Forma parte de la llave primaria clusterizada.</summary>
    public DateTime FechaEmision { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    /// <summary>Usuario que registro la orden. Se toma del JWT, nunca del cliente.</summary>
    public int UserId { get; set; }
    public User? User { get; set; }

    public string ProveedorNit { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;

    /// <summary>
    /// Total de la orden. Lo mantiene el trigger TR_OC_Detalle_Presupuesto como la suma del
    /// Importe de las lineas vigentes (EstadoLinea = 1); el backend no lo calcula.
    /// </summary>
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "GTQ";
    public string Estado { get; set; } = string.Empty;
    public string BodegaDestino { get; set; } = string.Empty;
    public string? Detalle { get; set; }

    public DateTime CreatedAt { get; set; }

    // Soft delete (Estado = ANULADA): quien, cuando y por que. Nunca hay DELETE fisico.
    public int? UsuarioAnulaId { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }

    /// <summary>Token de concurrencia optimista (rowversion). Cambia tambien cuando el trigger recalcula Monto.</summary>
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public ICollection<OrdenCompraDetalle> Lineas { get; set; } = new List<OrdenCompraDetalle>();
}
