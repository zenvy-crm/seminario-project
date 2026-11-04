namespace Seminario.Domain.Entities;

/// <summary>
/// Linea del detalle de una orden de compra. Vive en la misma particion que su encabezado
/// (misma FechaEmision). Nunca se borra fisicamente: la baja es EstadoLinea = false, lo que
/// dispara TR_OC_Detalle_Presupuesto y devuelve el importe al presupuesto de la bodega.
/// </summary>
public class OrdenCompraDetalle
{
    /// <summary>Columna de particion; igual a la FechaEmision del encabezado.</summary>
    public DateTime FechaEmision { get; set; }

    public Guid OrdenId { get; set; }
    public OrdenCompra? Orden { get; set; }

    /// <summary>Numero de linea dentro de la orden (1..N), asignado por el SP.</summary>
    public int Linea { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    /// <summary>Columna calculada persistida: ROUND(Cantidad * PrecioUnitario, 2).</summary>
    public decimal Importe { get; set; }

    /// <summary>true = vigente; false = anulada logicamente (soft delete de la linea).</summary>
    public bool EstadoLinea { get; set; } = true;

    public int? UsuarioBajaId { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string? MotivoBaja { get; set; }

    /// <summary>Token de concurrencia optimista de la linea (rowversion).</summary>
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}
