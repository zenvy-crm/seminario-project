using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Seminario.Infrastructure.MongoDB;

/// <summary>
/// Documento de la coleccion audit_logs. Los nombres de campo son los que exige la consigna de la
/// Fase 2 (fecha_evento, modulo, accion, entidad_afectada_id, jwt_usuario_ejecutor, rol_auth,
/// justificacion_cambio, datos_anteriores, datos_nuevos); el resto conserva el estilo snake_case.
/// El orden de las propiedades es el orden de los campos dentro del documento.
/// </summary>
public class AuditLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("fecha_evento")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; }

    [BsonElement("modulo")]
    public string Entity { get; set; } = string.Empty;

    [BsonElement("accion")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("entidad_afectada_id")]
    public string? EntityId { get; set; }

    [BsonElement("jwt_usuario_ejecutor")]
    public string? ActorUsername { get; set; }

    [BsonElement("rol_auth")]
    [BsonIgnoreIfNull]
    public string? RolAuth { get; set; }

    [BsonElement("justificacion_cambio")]
    [BsonIgnoreIfNull]
    public string? Justificacion { get; set; }

    [BsonElement("datos_anteriores")]
    [BsonIgnoreIfNull]
    public BsonDocument? Before { get; set; }

    [BsonElement("datos_nuevos")]
    [BsonIgnoreIfNull]
    public BsonDocument? After { get; set; }

    [BsonElement("actor_id")]
    [BsonIgnoreIfNull]
    public int? ActorUserId { get; set; }

    [BsonElement("usuario_afectado_id")]
    [BsonIgnoreIfNull]
    public int? UserId { get; set; }

    [BsonElement("usuario_afectado")]
    [BsonIgnoreIfNull]
    public string? Username { get; set; }

    [BsonElement("ip_origen")]
    [BsonIgnoreIfNull]
    public string? IpAddress { get; set; }
}
