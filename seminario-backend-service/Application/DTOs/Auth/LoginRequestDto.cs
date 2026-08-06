using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria")]
    public string Password { get; set; } = string.Empty;
}
