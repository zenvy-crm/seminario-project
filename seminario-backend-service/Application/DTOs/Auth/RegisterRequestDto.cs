using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [MaxLength(50)]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "El nombre de usuario solo puede contener letras, numeros, puntos y guiones bajos")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    public string? BirthDate { get; set; }

    [MaxLength(50)]
    public string? Identification { get; set; }

    [Required(ErrorMessage = "La contrasena es obligatoria")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contrasena")]
    [Compare(nameof(Password), ErrorMessage = "Las contrasenas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
