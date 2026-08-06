using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Seminario.Infrastructure.MongoDB;

public class AuditBackgroundService : BackgroundService
{
    private readonly AuditChannel _channel;
    private readonly IMongoCollection<AuditLogDocument> _collection;
    private readonly ILogger<AuditBackgroundService> _logger;

    public AuditBackgroundService(AuditChannel channel, IOptions<MongoSettings> mongoOptions, ILogger<AuditBackgroundService> logger)
    {
        _channel = channel;
        _logger = logger;

        var settings = mongoOptions.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<AuditLogDocument>(settings.AuditCollection);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var document = new AuditLogDocument
                {
                    EventType = evt.EventType,
                    Entity = evt.Entity,
                    EntityId = evt.EntityId,
                    UserId = evt.UserId,
                    Username = evt.Username,
                    IpAddress = evt.IpAddress,
                    Timestamp = evt.Timestamp,
                    Changes = (evt.Before is null && evt.After is null)
                        ? null
                        : new AuditChanges { Before = evt.Before, After = evt.After }
                };

                await _collection.InsertOneAsync(document, cancellationToken: stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Una falla temporal de MongoDB no debe afectar la operacion principal ya finalizada.
                _logger.LogError(ex, "No se pudo escribir el evento de auditoria {EventType} en MongoDB", evt.EventType);
            }
        }
    }
}
