namespace Seminario.Infrastructure.Persistence;

/// <summary>
/// Nombres de los objetos de SQL Server de la Fase 2 (SP, triggers, secuencia) y codigos de error
/// que lanzan con THROW. Los crea la migracion Fase2OrdenCompraDetalle con SQL nativo; aqui solo
/// se centralizan para que servicios y configuraciones EF no repitan cadenas.
/// </summary>
public static class OrdenCompraSqlObjects
{
    public const string TablaEncabezado = "TB_Ordenes_Compra";
    public const string TablaDetalle = "TB_Ordenes_Compra_Detalle";
    public const string TablaPresupuesto = "TB_Presupuesto_Bodega";

    /// <summary>dbo.SP_OrdenCompra_Crear @Json nvarchar(max), @UserId int.</summary>
    public const string SpCrear = "SP_OrdenCompra_Crear";

    /// <summary>Secuencia que alimenta NumeroOrden (OC-yyyy-NNNNNN).</summary>
    public const string SecuenciaNumeroOrden = "SEQ_OrdenCompra_Numero";

    public const string TriggerDetallePresupuesto = "TR_OC_Detalle_Presupuesto";
    public const string TriggerDetalleNoDelete = "TR_OC_Detalle_NoDelete";
    public const string TriggerEncabezadoAnulacion = "TR_OC_Encabezado_Anulacion";
    public const string TriggerEncabezadoNoDelete = "TR_OC_Encabezado_NoDelete";

    // Codigos de error (THROW) que el backend traduce a respuestas HTTP.
    /// <summary>No existe presupuesto para (anio, bodega).</summary>
    public const int ErrorSinPresupuesto = 50010;
    /// <summary>JSON invalido, sin lineas, o linea con cantidad &lt;= 0 / precio &lt; 0 / sin descripcion /
    /// decimales de mas / importe fuera de decimal(18,2).</summary>
    public const int ErrorLineaInvalida = 50011;
    /// <summary>El presupuesto disponible de la bodega quedaria negativo.</summary>
    public const int ErrorPresupuestoInsuficiente = 50012;
    /// <summary>Se intento modificar lineas de una orden ANULADA.</summary>
    public const int ErrorOrdenAnulada = 50013;
    /// <summary>DELETE fisico prohibido (triggers INSTEAD OF DELETE).</summary>
    public const int ErrorBorradoFisico = 50020;
    /// <summary>Una orden ANULADA no puede volver a otro estado.</summary>
    public const int ErrorReversionAnulacion = 50021;
    /// <summary>No se puede cambiar BodegaDestino de una orden con lineas vigentes.</summary>
    public const int ErrorCambioBodega = 50022;

    /// <summary>CHECK (MontoComprometido &gt;= 0) de TB_Presupuesto_Bodega.</summary>
    public const string CheckComprometido = "CK_Presupuesto_Bodega_Comprometido";
}
