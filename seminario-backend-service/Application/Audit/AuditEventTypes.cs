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

    public const string DocumentUploaded = "DOCUMENT_UPLOADED";
    public const string DocumentRejected = "DOCUMENT_REJECTED";
    public const string DocumentDeleted = "DOCUMENT_DELETED";
}
