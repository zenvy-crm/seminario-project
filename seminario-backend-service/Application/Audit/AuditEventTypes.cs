namespace Seminario.Application.Audit;

public static class AuditEventTypes
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string MfaSuccess = "MFA_SUCCESS";
    public const string MfaFailed = "MFA_FAILED";
    public const string Logout = "LOGOUT";
    public const string AccountDisabled = "ACCOUNT_DISABLED";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountUnlocked = "ACCOUNT_UNLOCKED";

    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserRoleChanged = "USER_ROLE_CHANGED";
    public const string UserStatusChanged = "USER_STATUS_CHANGED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordRehashed = "PASSWORD_REHASHED";

    public const string DocumentUploaded = "DOCUMENT_UPLOADED";
    public const string DocumentRejected = "DOCUMENT_REJECTED";
    public const string DocumentDeleted = "DOCUMENT_DELETED";

    public const string OrdenCompraCreada = "ORDEN_COMPRA_CREADA";
    /// <summary>Soft delete de una linea del detalle (EstadoLinea = 0) por un Intermedio o Administrador.</summary>
    public const string LineaAnulada = "LINEA_ANULADA";
    /// <summary>Soft delete de la orden completa (Estado = ANULADA) por el Administrador.</summary>
    public const string AnulacionMaster = "ANULACION_MASTER";
    public const string SeedDataEjecutado = "SEED_DATA_EJECUTADO";

    /// <summary>Consulta de los reportes de la Fase 3 (ranking de proveedores y reporte de ordenes), con los filtros usados.</summary>
    public const string ReporteConsultado = "REPORTE_CONSULTADO";
    /// <summary>Consulta del reporte de auditoria de soft delete (solo Administrador).</summary>
    public const string ReporteAuditoriaConsultado = "REPORTE_AUDITORIA_CONSULTADO";
}
