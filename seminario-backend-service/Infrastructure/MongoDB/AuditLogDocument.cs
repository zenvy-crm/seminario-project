using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Seminario.Infrastructure.MongoDB;

public class AuditLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }

    public AuditChanges? Changes { get; set; }

    public DateTime Timestamp { get; set; }
}

public class AuditChanges
{
    public object? Before { get; set; }
    public object? After { get; set; }
}
