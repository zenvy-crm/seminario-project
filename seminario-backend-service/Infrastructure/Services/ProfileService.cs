using Microsoft.AspNetCore.Identity;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Profile;
using Seminario.Application.Interfaces;
using Seminario.Application.Mappings;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ProfileService(IUserRepository userRepository, IAuditLogger auditLogger)
    {
        _userRepository = userRepository;
        _auditLogger = auditLogger;
    }

    public async Task<UserDto> GetAsync(int userId)
    {
        var user = await _userRepository.GetByIdWithDocumentsAsync(userId)
            ?? throw new NotFoundException("El usuario no existe");
        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("El usuario no existe");

        var before = new Dictionary<string, object?>();
        var after = new Dictionary<string, object?>();

        if (dto.Email is not null && dto.Email != user.Email)
        {
            if (await _userRepository.EmailExistsAsync(dto.Email, userId))
            {
                throw new ConflictException("El correo ya esta registrado por otro usuario");
            }
            before["email"] = user.Email;
            after["email"] = dto.Email;
            user.Email = dto.Email;
        }

        if (dto.FirstName is not null && dto.FirstName != user.FirstName)
        {
            before["firstName"] = user.FirstName;
            after["firstName"] = dto.FirstName;
            user.FirstName = dto.FirstName;
        }

        if (dto.LastName is not null && dto.LastName != user.LastName)
        {
            before["lastName"] = user.LastName;
            after["lastName"] = dto.LastName;
            user.LastName = dto.LastName;
        }

        if (dto.Phone is not null && dto.Phone != user.Phone)
        {
            before["phone"] = user.Phone;
            after["phone"] = dto.Phone;
            user.Phone = dto.Phone;
        }

        if (dto.Identification is not null && dto.Identification != user.IdentificationNumber)
        {
            before["identification"] = user.IdentificationNumber;
            after["identification"] = dto.Identification;
            user.IdentificationNumber = dto.Identification;
        }

        if (before.Count > 0)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();

            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.UserUpdated,
                Entity = "User",
                EntityId = user.Id.ToString(),
                UserId = user.Id,
                Username = user.Username,
                Before = before,
                After = after
            });
        }

        return user.ToDto();
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("El usuario no existe");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedApiException("La contrasena actual es incorrecta");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.PasswordChanged,
            Entity = "User",
            EntityId = user.Id.ToString(),
            UserId = user.Id,
            Username = user.Username
        });
    }
}
