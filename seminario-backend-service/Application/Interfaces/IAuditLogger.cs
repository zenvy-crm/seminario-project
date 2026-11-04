using Seminario.Application.Audit;

namespace Seminario.Application.Interfaces;

public interface IAuditLogger
{
    /// <summary>Encola el evento en el canal en memoria; AuditBackgroundService lo escribe en MongoDB.</summary>
    void Enqueue(AuditEvent auditEvent);

    /// <summary>
    /// Escribe el evento en MongoDB en un hilo en segundo plano (Task.Run) sin que la peticion HTTP
    /// lo espere. La ip, el ejecutor y el rol se toman del HttpContext ANTES de lanzar el hilo.
    /// Reintenta ante fallas de MongoDB y, si se agotan los intentos, lo registra en el log.
    /// </summary>
    void FireAndForget(AuditEvent auditEvent);
}
