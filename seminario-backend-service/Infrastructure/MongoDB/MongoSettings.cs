namespace Seminario.Infrastructure.MongoDB;

public class MongoSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AuditCollection { get; set; } = "audit_logs";
}
