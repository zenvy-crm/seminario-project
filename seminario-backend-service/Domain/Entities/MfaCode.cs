namespace Seminario.Domain.Entities;

public class MfaCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string MfaToken { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;

    public int Attempts { get; set; }
    public const int MaxAttempts = 5;

    public bool IsUsed { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
