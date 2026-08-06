using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Auth;

public class MfaVerifyRequestDto
{
    [Required(ErrorMessage = "El token de MFA es obligatorio")]
    public string MfaToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "El codigo es obligatorio")]
    public string Code { get; set; } = string.Empty;
}
