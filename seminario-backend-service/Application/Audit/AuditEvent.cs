namespace Seminario.Application.Audit;

public class AuditEvent
{
    /// <summary>Accion auditada (campo "accion" en MongoDB), ej. ANULACION_MASTER.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Modulo o entidad (campo "modulo" en MongoDB), ej. ORDEN_COMPRA.</summary>
    public string Entity { get; set; } = string.Empty;

    /// <summary>Identificador de lo afectado (campo "entidad_afectada_id"), ej. OC-2026-000482.</summary>
    public string? EntityId { get; set; }

    /// <summary>Usuario afectado por el evento (no necesariamente quien lo ejecuto).</summary>
    public int? UserId { get; set; }
    public string? Username { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>Usuario autenticado que ejecuto la accion (ej. el ADMIN). Se completa automaticamente si no se especifica.</summary>
    public int? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }

    /// <summary>
    /// Rol del ejecutor segun la Fase 2: Estandar / Intermedio / Administrador (campo "rol_auth").
    /// Si viene vacio se deriva del claim de rol del JWT de la peticion.
    /// </summary>
    public string? RolAuth { get; set; }

    /// <summary>Motivo que el usuario escribio para el cambio (campo "justificacion_cambio").</summary>
    public string? Justificacion { get; set; }

    /// <summary>Estado previo (campo "datos_anteriores").</summary>
    public object? Before { get; set; }

    /// <summary>Estado nuevo (campo "datos_nuevos").</summary>
    public object? After { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
