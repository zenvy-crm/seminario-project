using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Seminario.Controllers;

/// <summary>
/// Patron Health Check. Dos endpoints anonimos:
/// - GET /api/health: readiness. Ejecuta las sondas de SQL Server y MongoDB y devuelve
///   el estado agregado con metricas del proceso. 200 si esta Healthy o Degraded,
///   503 si esta Unhealthy. Es el que vigila Uptime Kuma.
/// - GET /api/health/live: liveness. Solo confirma que el proceso atiende peticiones;
///   lo usa el HEALTHCHECK del contenedor para no marcarlo caido por culpa de una base.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private const string Servicio = "seminario-backend-service";
    private static readonly DateTime StartedAtUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();
    private static readonly string Version =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    private readonly HealthCheckService _healthChecks;
    private readonly IWebHostEnvironment _env;

    public HealthController(HealthCheckService healthChecks, IWebHostEnvironment env)
    {
        _healthChecks = healthChecks;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var reporte = await _healthChecks.CheckHealthAsync(cancellationToken);
        var proceso = Process.GetCurrentProcess();

        var cuerpo = new
        {
            status = reporte.Status.ToString(),
            service = Servicio,
            version = Version,
            environment = _env.EnvironmentName,
            timestamp = DateTime.UtcNow,
            uptimeSeconds = (long)(DateTime.UtcNow - StartedAtUtc).TotalSeconds,
            totalDurationMs = Math.Round(reporte.TotalDuration.TotalMilliseconds, 1),
            checks = reporte.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1)
            }),
            process = new
            {
                memoryMb = Math.Round(proceso.WorkingSet64 / 1024d / 1024d, 1),
                gcHeapMb = Math.Round(GC.GetTotalMemory(false) / 1024d / 1024d, 1),
                threads = proceso.Threads.Count
            }
        };

        return reporte.Status == HealthStatus.Unhealthy
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, cuerpo)
            : Ok(cuerpo);
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "ok",
            service = Servicio,
            timestamp = DateTime.UtcNow,
            uptimeSeconds = (long)(DateTime.UtcNow - StartedAtUtc).TotalSeconds
        });
    }
}
