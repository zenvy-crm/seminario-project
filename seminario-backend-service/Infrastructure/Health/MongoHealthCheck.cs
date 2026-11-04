using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Seminario.Infrastructure.MongoDB;

namespace Seminario.Infrastructure.Health;

/// <summary>
/// Sonda de MongoDB: envia un "ping" a la base de auditoria.
/// Se registra con FailureStatus = Degraded y no Unhealthy: la bitacora es asincrona
/// y su caida no debe impedir operar (regla del DERCAS), asi que la API sigue
/// respondiendo 200 pero avisa que la auditoria no esta disponible.
/// </summary>
public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IOptions<MongoSettings> options)
    {
        // Cliente propio con seleccion de servidor corta: con el default (30 s) una
        // caida de Mongo haria que /api/health tardara mas que el timeout del monitor.
        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        settings.ConnectTimeout = TimeSpan.FromSeconds(3);
        _database = new MongoClient(settings).GetDatabase(options.Value.DatabaseName);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var reloj = Stopwatch.StartNew();

        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB responde",
                new Dictionary<string, object> { ["latencyMs"] = reloj.ElapsedMilliseconds });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "No se pudo conectar a MongoDB (la auditoria se reintentara)");
        }
    }
}
