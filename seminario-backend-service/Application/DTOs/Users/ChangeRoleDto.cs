using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Users;

public class ChangeRoleDto
{
    [Required(ErrorMessage = "El rol es obligatorio")]
    public string Role { get; set; } = string.Empty;
}
