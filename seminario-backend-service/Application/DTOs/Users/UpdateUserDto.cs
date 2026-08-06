using System.ComponentModel.DataAnnotations;

namespace Seminario.Application.DTOs.Users;

public class UpdateUserDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    public string? BirthDate { get; set; }

    [MaxLength(50)]
    public string? Identification { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }
}
