namespace Seminario.Domain.Entities;

/// <summary>
/// Presupuesto de compras por bodega y anio: es la "capacidad" que descuentan las lineas de
/// las ordenes. MontoComprometido lo mantienen exclusivamente los triggers de SQL Server.
/// </summary>
public class PresupuestoBodega
{
    public short Anio { get; set; }
    public string Bodega { get; set; } = string.Empty;

    public decimal MontoAsignado { get; set; }
    public decimal MontoComprometido { get; set; }

    /// <summary>Columna calculada persistida: MontoAsignado - MontoComprometido.</summary>
    public decimal MontoDisponible { get; set; }

    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}
