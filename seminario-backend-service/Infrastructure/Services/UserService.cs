using System.Globalization;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Users;
using Seminario.Application.Interfaces;
using Seminario.Application.Mappings;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;

namespace Seminario.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IAuditLogger auditLogger, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _auditLogger = auditLogger;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, total) = await _userRepository.GetPagedAsync(
            query.Search, query.Role, query.Status, page, pageSize, query.SortBy, query.SortDir);

        return new PagedResult<UserDto>
        {
            Items = items.Select(u => u.ToDto()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdWithDocumentsAsync(id)
            ?? throw new NotFoundException("El usuario no existe");
        return user.ToDto();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        if (await _userRepository.UsernameExistsAsync(dto.Username))
        {
            throw new ConflictException("El nombre de usuario ya esta en uso");
        }

        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            throw new ConflictException("El correo ya esta registrado");
        }

        var roleName = string.IsNullOrWhiteSpace(dto.Role) ? RoleName.User : dto.Role.ToUpperInvariant();
        var role = await _userRepository.GetRoleByNameAsync(roleName)
            ?? throw new ValidationApiException("El rol especificado no es valido", new List<string> { "role" });

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            Phone = dto.Phone,
            IdentificationNumber = dto.Identification,
            BirthDate = DateOnly.TryParse(dto.BirthDate, out var parsedBirth) ? parsedBirth : null,
            RoleId = role.Id,
            Status = UserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.Hash(dto.Password);

        await _userRepository.AddAsync(user);
        user.Role = role;

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.UserCreated,
            Entity = "User",
            EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
            UserId = user.Id,
            Username = user.Username,
            After = new { user.Username, user.Email, Role = role.Name }
        });

        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("El usuario no existe");

        var before = new Dictionary<string, object?>();
        var after = new Dictionary<string, object?>();

        if (dto.Email is not null && dto.Email != user.Email)
        {
            if (await _userRepository.EmailExistsAsync(dto.Email, id))
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

        if (dto.BirthDate is not null && DateOnly.TryParse(dto.BirthDate, out var parsedBirth) && parsedBirth != user.BirthDate)
        {
            before["birthDate"] = user.BirthDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            after["birthDate"] = dto.BirthDate;
            user.BirthDate = parsedBirth;
        }

        if (before.Count > 0)
        {
            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.UserUpdated,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username,
                Before = before,
                After = after
            });
        }

        if (!string.IsNullOrWhiteSpace(dto.Role) && !string.Equals(dto.Role, user.Role.Name, StringComparison.OrdinalIgnoreCase))
        {
            var newRole = await _userRepository.GetRoleByNameAsync(dto.Role.ToUpperInvariant())
                ?? throw new ValidationApiException("El rol especificado no es valido", new List<string> { "role" });

            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.UserRoleChanged,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username,
                Before = new { role = user.Role.Name },
                After = new { role = newRole.Name }
            });

            user.RoleId = newRole.Id;
            user.Role = newRole;
        }

        if (!string.IsNullOrWhiteSpace(dto.Status) && Enum.TryParse<UserStatus>(dto.Status, true, out var newStatus) && newStatus != user.Status)
        {
            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.UserStatusChanged,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username,
                Before = new { status = user.Status.ToString() },
                After = new { status = newStatus.ToString() }
            });

            user.Status = newStatus;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        return user.ToDto();
    }

    public async Task<UserDto> UnlockAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("El usuario no existe");

        if (user.LockedUntil is null && user.FailedLoginAttempts == 0)
        {
            return user.ToDto();
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.AccountUnlocked,
            Entity = "User",
            EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
            UserId = user.Id,
            Username = user.Username,
            After = new { reason = "manual" }
        });

        return user.ToDto();
    }
}
