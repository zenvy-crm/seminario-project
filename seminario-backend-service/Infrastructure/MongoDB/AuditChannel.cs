using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Seminario.Api;
using Seminario.Application.Audit;
using Seminario.Application.Interfaces;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.Logging;

namespace Seminario.Infrastructure.MongoDB;

public class AuditChannel : IAuditLogger
{
    /// <summary>Espera antes de cada reintento de FireAndForget: 1 intento inicial + 3 reintentos.</summary>
    private static readonly TimeSpan[] EsperasReintento =
    {
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3)
    };

    private readonly Channel<AuditEvent> _channel = Channel.CreateUnbounded<AuditEvent>();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditStore _store;
    private readonly ILogger<AuditChannel> _logger;

    public AuditChannel(IHttpContextAccessor httpContextAccessor, IAuditStore store, ILogger<AuditChannel> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _store = store;
        _logger = logger;
    }

    public ChannelReader<AuditEvent> Reader => _channel.Reader;

    public void Enqueue(AuditEvent auditEvent)
    {
        CompletarDesdeContexto(auditEvent);
        _channel.Writer.TryWrite(auditEvent);
    }

    public void FireAndForget(AuditEvent auditEvent)
    {
        // El HttpContext solo es valido durante la peticion: se lee AHORA, antes de soltar el hilo.
        CompletarDesdeContexto(auditEvent);

        // Hilo en segundo plano: la respuesta HTTP no espera a MongoDB.
        _ = Task.Run(async () =>
        {
            for (var intento = 0; ; intento++)
            {
                try
                {
                    await _store.InsertAsync(auditEvent);
                    return;
                }
                catch (Exception ex) when (intento < EsperasReintento.Length)
                {
                    _logger.AuditoriaReintento(ex, intento + 1, auditEvent.EventType, auditEvent.EntityId, EsperasReintento[intento].TotalMilliseconds);
                    await Task.Delay(EsperasReintento[intento]);
                }
                catch (Exception ex)
                {
                    _logger.AuditoriaDescartada(ex, auditEvent.EventType, auditEvent.EntityId, intento + 1);
                    return;
                }
            }
        });
    }

    /// <summary>Completa ip, ejecutor y rol_auth con los datos de la peticion cuando no vienen en el evento.</summary>
    private void CompletarDesdeContexto(AuditEvent auditEvent)
    {
        var context = _httpContextAccessor.HttpContext;

        auditEvent.IpAddress ??= GetClientIpAddress(context);

        if (context?.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (auditEvent.ActorUserId is null)
        {
            auditEvent.ActorUserId = context.User.GetUserId();
            auditEvent.ActorUsername = context.User.FindFirst("username")?.Value;
        }

        if (string.IsNullOrWhiteSpace(auditEvent.RolAuth))
        {
            var rol = context.User.GetRoleName();
            auditEvent.RolAuth = string.IsNullOrEmpty(rol) ? null : RoleName.RolAuthDe(rol);
        }
    }

    private static string? GetClientIpAddress(HttpContext? context)
    {
        if (context is null)
        {
            return null;
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
