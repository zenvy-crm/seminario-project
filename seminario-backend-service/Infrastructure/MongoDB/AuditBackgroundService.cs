using Seminario.Application.Interfaces;
using Seminario.Infrastructure.Logging;

namespace Seminario.Infrastructure.MongoDB;

/// <summary>Drena el canal en memoria de auditoria y escribe cada evento en MongoDB.</summary>
public class AuditBackgroundService : BackgroundService
{
    private readonly AuditChannel _channel;
    private readonly IAuditStore _store;
    private readonly ILogger<AuditBackgroundService> _logger;

    public AuditBackgroundService(AuditChannel channel, IAuditStore store, ILogger<AuditBackgroundService> logger)
    {
        _channel = channel;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _store.InsertAsync(evt, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.AuditoriaNoEscrita(ex, evt.EventType);
            }
        }
    }
}
