namespace Seminario.Application.DTOs.Auth;

public class AuthUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AuthDataDto
{
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public AuthUserDto? User { get; set; }
    public string? MfaToken { get; set; }
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public bool? RequiresMfa { get; set; }
    public string? Message { get; set; }
    public AuthDataDto Data { get; set; } = new();
}
