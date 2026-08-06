using System.Threading.Channels;
using Seminario.Application.Audit;
using Seminario.Application.Interfaces;

namespace Seminario.Infrastructure.MongoDB;

public class AuditChannel : IAuditLogger
{
    private readonly Channel<AuditEvent> _channel = Channel.CreateUnbounded<AuditEvent>();

    public ChannelReader<AuditEvent> Reader => _channel.Reader;

    public void Enqueue(AuditEvent auditEvent)
    {
        _channel.Writer.TryWrite(auditEvent);
    }
}
