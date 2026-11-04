namespace Seminario.Domain.Enums;

/// <summary>Estados del ciclo de vida de una orden de compra a proveedor.</summary>
public static class OrdenEstado
{
    /// <summary>Estado inicial: toda orden creada por SP_OrdenCompra_Crear nace CREADA.</summary>
    public const string Creada = "CREADA";
    public const string Emitida = "EMITIDA";
    public const string Recibida = "RECIBIDA";
    public const string Pagada = "PAGADA";

    /// <summary>Soft delete: la orden sigue en la base de datos pero se oculta al rol Estandar.</summary>
    public const string Anulada = "ANULADA";

    public static readonly string[] Todos = { Creada, Emitida, Recibida, Pagada, Anulada };

    public static bool EsValido(string estado) => Todos.Contains(estado);
}
