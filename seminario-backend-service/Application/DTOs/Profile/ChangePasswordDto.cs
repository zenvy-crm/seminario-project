using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Profile;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contrasena actual es obligatoria")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contrasena es obligatoria")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}
