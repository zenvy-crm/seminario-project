using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Seminario.Api;
using Seminario.Application.Audit;
using Seminario.Application.Interfaces;

namespace Seminario.Infrastructure.MongoDB;

public class AuditChannel : IAuditLogger
{
    private readonly Channel<AuditEvent> _channel = Channel.CreateUnbounded<AuditEvent>();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditChannel(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ChannelReader<AuditEvent> Reader => _channel.Reader;

    public void Enqueue(AuditEvent auditEvent)
    {
        var context = _httpContextAccessor.HttpContext;

        auditEvent.IpAddress ??= GetClientIpAddress(context);

        if (auditEvent.ActorUserId is null && context?.User.Identity?.IsAuthenticated == true)
        {
            auditEvent.ActorUserId = context.User.GetUserId();
            auditEvent.ActorUsername = context.User.FindFirst("username")?.Value;
        }

        _channel.Writer.TryWrite(auditEvent);
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
