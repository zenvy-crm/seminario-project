using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Operaciones;

/// <summary>
/// Datos que el formulario web envia para registrar una orden. Notese que NO incluye
/// el usuario (se extrae del JWT en el servidor) ni el estado: toda orden nace CREADA
/// dentro de SP_OrdenCompra_Crear. Si el cliente manda "estado", se ignora.
/// </summary>
public class CreateOrdenCompraDto
{
    [Required(ErrorMessage = "El NIT del proveedor es obligatorio")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "El NIT debe tener entre 4 y 20 caracteres")]
    [RegularExpression(@"^[0-9Kk\-]+$", ErrorMessage = "El NIT solo admite digitos, guion y la letra K")]
    public string ProveedorNit { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del proveedor es obligatorio")]
    [StringLength(150, MinimumLength = 3)]
    public string ProveedorNombre { get; set; } = string.Empty;

    /// <summary>
    /// Opcional. Solo se usa por compatibilidad con el formulario actual: si NO vienen lineas y
    /// Monto &gt; 0 se crea una unica linea "Monto global" (cantidad 1, precio = Monto).
    /// Si vienen lineas se ignora: el total lo calcula el trigger del detalle.
    /// </summary>
    [Range(0, 99999999.99, ErrorMessage = "El monto debe estar entre 0 y 99,999,999.99")]
    public decimal? Monto { get; set; }

    [Required]
    [RegularExpression("^(GTQ|USD)$", ErrorMessage = "La moneda debe ser GTQ o USD")]
    public string Moneda { get; set; } = "GTQ";

    /// <summary>Opcional: si no viene se usa la fecha UTC de hoy.</summary>
    public DateTime? FechaEmision { get; set; }

    [Required(ErrorMessage = "La bodega destino es obligatoria")]
    [StringLength(60, MinimumLength = 2)]
    public string BodegaDestino { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Detalle { get; set; }

    /// <summary>Lineas del detalle; el SP las numera 1..N en el orden recibido.</summary>
    [MaxLength(200, ErrorMessage = "La orden admite maximo 200 lineas")]
    public List<CreateLineaOrdenDto>? Lineas { get; set; }
}

/// <summary>
/// Una linea del detalle. Cantidad y PrecioUnitario NO llevan [Range] a proposito: los valida
/// SP_OrdenCompra_Crear (cantidad &gt; 0, precio &gt;= 0, maximo 3 / 4 decimales, importe dentro de
/// decimal(18,2)). Asi una linea invalida llega al SP, que ya inserto el encabezado, y se ve el
/// ROLLBACK absoluto: responde 422 y no queda ni encabezado ni lineas.
/// </summary>
public class CreateLineaOrdenDto
{
    [Required(ErrorMessage = "La descripcion de la linea es obligatoria")]
    [StringLength(150, ErrorMessage = "La descripcion de la linea admite maximo 150 caracteres")]
    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}

public class OrdenCompraDto
{
    public Guid Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public int UserId { get; set; }
    public string ProveedorNit { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;

    /// <summary>Total mantenido por el trigger: suma del Importe de las lineas vigentes.</summary>
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string BodegaDestino { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime CreatedAt { get; set; }

    // Soft delete de la orden (Estado = ANULADA).
    public int? UsuarioAnulaId { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }

    /// <summary>rowversion en base64: se reenvia en PUT/DELETE para el bloqueo optimista.</summary>
    public string RowVer { get; set; } = string.Empty;

    /// <summary>Lineas del detalle, incluidas las anuladas (EstadoLinea = false). Vacio en el listado paginado.</summary>
    public List<LineaOrdenDto> Lineas { get; set; } = [];

    /// <summary>Presupuesto de la bodega/anio de la orden. Solo en el detalle, POST, PUT y DELETE; null en el listado.</summary>
    public PresupuestoBodegaDto? Presupuesto { get; set; }
}

public class LineaOrdenDto
{
    public int Linea { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    /// <summary>Columna calculada: ROUND(Cantidad * PrecioUnitario, 2).</summary>
    public decimal Importe { get; set; }

    /// <summary>true = vigente; false = anulada logicamente.</summary>
    public bool EstadoLinea { get; set; }
    public int? UsuarioBajaId { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string? MotivoBaja { get; set; }

    /// <summary>rowversion de la linea en base64 (bloqueo optimista del PUT).</summary>
    public string RowVer { get; set; } = string.Empty;
}

/// <summary>
/// Cuerpo del PUT /ordenes/{id}/lineas/{linea}: soft delete de una linea (EstadoLinea = 0).
/// El motivo es obligatorio, pero puede venir aqui o en ?motivo=; el servicio lo valida ya recortado
/// (entre 3 y 200 caracteres).
/// </summary>
public class AnularLineaDto
{
    [StringLength(200, ErrorMessage = "El motivo admite maximo 200 caracteres")]
    public string? Motivo { get; set; }

    /// <summary>Opcional, tal como lo describe la consigna: si viene, solo se admite 0 (anular la linea).</summary>
    public int? EstadoLinea { get; set; }

    /// <summary>Opcional: rowVer (base64) de la LINEA leida por el cliente. Si no coincide, 409.</summary>
    public string? RowVer { get; set; }
}

/// <summary>
/// Cuerpo (opcional) del DELETE /ordenes/{id}: soft delete de la orden (Estado = ANULADA).
/// La justificacion es obligatoria, pero puede venir aqui o en ?motivo= (un DELETE sin cuerpo);
/// el servicio la valida ya recortada (entre 3 y 200 caracteres).
/// </summary>
public class AnularOrdenDto
{
    [StringLength(200, ErrorMessage = "El motivo admite maximo 200 caracteres")]
    public string? Motivo { get; set; }

    /// <summary>Opcional: rowVer (base64) de la ORDEN leida por el cliente. Si no coincide, 409.</summary>
    public string? RowVer { get; set; }
}

/// <summary>Una fila de TB_Presupuesto_Bodega: la "capacidad" que consumen las lineas.</summary>
public class PresupuestoBodegaDto
{
    public int Anio { get; set; }
    public string Bodega { get; set; } = string.Empty;
    public decimal MontoAsignado { get; set; }
    public decimal MontoComprometido { get; set; }
    public decimal MontoDisponible { get; set; }
}

/// <summary>
/// Filtros del listado. El rango de fechas es lo que habilita la eliminacion de particiones.
/// Para el rol Estandar (USER) las ordenes ANULADA se excluyen siempre, aunque se pida Estado=ANULADA.
/// </summary>
public class OrdenCompraQueryDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Estado { get; set; }
    public string? ProveedorNit { get; set; }
    public bool? SoloMias { get; set; }

    /// <summary>Solo ordenes maestro-detalle (creadas por el SP). Excluye las filas del stress test, que no tienen lineas.</summary>
    public bool? ConDetalle { get; set; }

    /// <summary>Busqueda exacta por numero de orden (ej. OC-2026-000001).</summary>
    [StringLength(30)]
    public string? NumeroOrden { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Una fila de la auditoria de particiones (PASO 5 del script).</summary>
public class ParticionInfoDto
{
    public int NumeroParticion { get; set; }
    public string ArchivoFisico { get; set; } = string.Empty;
    public string RangoDesde { get; set; } = string.Empty;
    public string RangoHasta { get; set; } = string.Empty;
    public long RegistrosEnEsteFragmento { get; set; }
}

public class SeedDataRequestDto
{
    [Range(1, 500000, ErrorMessage = "La cantidad debe estar entre 1 y 500,000")]
    public int Cantidad { get; set; } = 50000;

    [Range(2020, 2030)]
    public int AnioInicial { get; set; } = 2024;

    [Range(2020, 2030)]
    public int AnioFinal { get; set; } = 2026;
}

public class SeedDataResultDto
{
    public int RegistrosInsertados { get; set; }
    public long DuracionMs { get; set; }
    public int Lotes { get; set; }
    public double RegistrosPorSegundo { get; set; }
    public List<ParticionInfoDto> Particiones { get; set; } = [];
}
