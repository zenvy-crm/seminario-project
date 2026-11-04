using System.Globalization;
using Seminario.Application.DTOs;
using Seminario.Domain.Entities;

namespace Seminario.Application.Mappings;

public static class UserMappings
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Username = user.Username,
        Email = user.Email,
        Phone = user.Phone,
        BirthDate = user.BirthDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Identification = user.IdentificationNumber,
        Role = user.Role.Name,
        Status = user.Status.ToString(),
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        IsLocked = user.LockedUntil > DateTime.UtcNow,
        LockedUntil = user.LockedUntil,
        Documents = user.Documents?.Select(d => new DocumentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            MimeType = d.ContentType,
            SizeBytes = d.FileSize,
            UploadedAt = d.CreatedAt,
            Url = $"/api/users/{d.UserId}/documents/{d.Id}/content"
        }).ToList()
    };
}
