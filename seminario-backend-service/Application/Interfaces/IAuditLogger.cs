using Seminario.Application.Audit;

namespace Seminario.Application.Interfaces;

public interface IAuditLogger
{
    void Enqueue(AuditEvent auditEvent);
}
