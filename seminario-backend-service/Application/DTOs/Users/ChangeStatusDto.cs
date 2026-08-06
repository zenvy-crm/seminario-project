using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Users;

public class ChangeStatusDto
{
    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Status { get; set; } = string.Empty;
}
