using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Seminario.Application.Audit;
using Seminario.Application.Interfaces;

namespace Seminario.Infrastructure.MongoDB;

/// <summary>
/// Inserta los eventos de auditoria en MongoDB. Lo usan el canal (AuditBackgroundService) y el hilo
/// en segundo plano de AuditChannel.FireAndForget. Es singleton: MongoClient es thread-safe.
/// </summary>
public class MongoAuditStore : IAuditStore
{
    private readonly IMongoCollection<AuditLogDocument> _collection;

    static MongoAuditStore()
    {
        // El driver 3.x no serializa Guid sin una representacion explicita (los datos anteriores/nuevos pueden traerlos).
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public MongoAuditStore(IOptions<MongoSettings> mongoOptions)
    {
        var settings = mongoOptions.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<AuditLogDocument>(settings.AuditCollection);
    }

    public Task InsertAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(ToDocument(auditEvent), cancellationToken: cancellationToken);

    /// <summary>Arma el documento con los nombres de campo exigidos (fecha_evento, modulo, accion, ...).</summary>
    public static AuditLogDocument ToDocument(AuditEvent auditEvent) => new()
    {
        Timestamp = auditEvent.Timestamp.Kind == DateTimeKind.Local
            ? auditEvent.Timestamp.ToUniversalTime()
            : DateTime.SpecifyKind(auditEvent.Timestamp, DateTimeKind.Utc),
        Entity = auditEvent.Entity,
        EventType = auditEvent.EventType,
        EntityId = auditEvent.EntityId,
        ActorUsername = auditEvent.ActorUsername,
        RolAuth = string.IsNullOrWhiteSpace(auditEvent.RolAuth) ? null : auditEvent.RolAuth,
        Justificacion = string.IsNullOrWhiteSpace(auditEvent.Justificacion) ? null : auditEvent.Justificacion,
        Before = ToBson(auditEvent.Before),
        After = ToBson(auditEvent.After),
        ActorUserId = auditEvent.ActorUserId,
        UserId = auditEvent.UserId,
        Username = auditEvent.Username,
        IpAddress = auditEvent.IpAddress
    };

    /// <summary>
    /// Convierte el objeto (normalmente anonimo) en un subdocumento usando su tipo real como tipo
    /// nominal: asi el driver no agrega el discriminador "_t" y los campos quedan como se nombraron.
    /// </summary>
    private static BsonDocument? ToBson(object? value) => value switch
    {
        null => null,
        BsonDocument bson => bson,
        _ => value.ToBsonDocument(value.GetType())
    };
}
