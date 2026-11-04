using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Health;

/// <summary>
/// Sonda de SQL Server: abre una conexion real con el mismo DbContext que usa la API.
/// Si falla, la API no puede operar (ordenes, usuarios, login), asi que se registra
/// con FailureStatus = Unhealthy y /api/health responde 503.
/// </summary>
public class SqlServerHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
    private readonly AppDbContext _db;

    public SqlServerHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);
        var reloj = Stopwatch.StartNew();

        try
        {
            if (await _db.Database.CanConnectAsync(cts.Token))
            {
                return HealthCheckResult.Healthy("SQL Server responde",
                    new Dictionary<string, object> { ["latencyMs"] = reloj.ElapsedMilliseconds });
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout o error de red: se reporta abajo. El detalle no se expone porque
            // el endpoint es anonimo y el mensaje podria revelar el host de la base.
        }

        return new HealthCheckResult(context.Registration.FailureStatus, "No se pudo conectar a SQL Server");
    }
}
