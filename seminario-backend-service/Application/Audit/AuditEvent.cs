namespace Seminario.Application.Audit;

public class AuditEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    /// <summary>Usuario afectado por el evento (no necesariamente quien lo ejecuto).</summary>
    public int? UserId { get; set; }
    public string? Username { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>Usuario autenticado que ejecuto la accion (ej. el ADMIN). Se completa automaticamente si no se especifica.</summary>
    public int? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }
    public object? Before { get; set; }
    public object? After { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
