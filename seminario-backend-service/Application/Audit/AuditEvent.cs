namespace Seminario.Application.Audit;

public class AuditEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public object? Before { get; set; }
    public object? After { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
