using Seminario.Application.Audit;

namespace Seminario.Application.Interfaces;

/// <summary>Almacen de la evidencia de auditoria (coleccion audit_logs de MongoDB).</summary>
public interface IAuditStore
{
    Task InsertAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
