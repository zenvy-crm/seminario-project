namespace Seminario.Infrastructure.Logging;

/// <summary>
/// Mensajes de log de alto rendimiento generados en compilacion ([LoggerMessage], CA1848/CA1873):
/// evitan el boxing de los argumentos y el parseo de la plantilla en cada llamada.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Error no controlado procesando {Path}")]
    public static partial void ErrorNoControlado(this ILogger logger, Exception exception, string path);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Codigo MFA para el usuario {UserId}: {Code} (valido {Minutes} min)")]
    public static partial void CodigoMfaEmitido(this ILogger logger, int userId, string code, int minutes);

    [LoggerMessage(EventId = 1200, Level = LogLevel.Error, Message = "No se pudo escribir el evento de auditoria {EventType} en MongoDB")]
    public static partial void AuditoriaNoEscrita(this ILogger logger, Exception exception, string eventType);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Warning,
        Message = "Fallo el intento {Intento} de guardar la auditoria {EventType} de {EntityId} en MongoDB; se reintenta en {Espera} ms")]
    public static partial void AuditoriaReintento(this ILogger logger, Exception exception, int intento, string eventType, string? entityId, double espera);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Error,
        Message = "No se pudo guardar la auditoria {EventType} de {EntityId} en MongoDB despues de {Intentos} intentos")]
    public static partial void AuditoriaDescartada(this ILogger logger, Exception exception, string eventType, string? entityId, int intentos);

    [LoggerMessage(EventId = 1300, Level = LogLevel.Information, Message = "Stress test: {Insertados}/{Total} ordenes insertadas ({Lotes} lotes)")]
    public static partial void StressTestAvance(this ILogger logger, int insertados, int total, int lotes);
}
