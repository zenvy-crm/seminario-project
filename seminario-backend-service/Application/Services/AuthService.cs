using System.Globalization;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs.Auth;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Domain.Enums;

namespace Seminario.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedLoginAttempts = 3;
    private const int LockoutMinutes = 5;

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ITokenService _tokenService;
    private readonly IMfaService _mfaService;
    private readonly IAuditLogger _auditLogger;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenStore refreshTokenStore,
        ITokenService tokenService,
        IMfaService mfaService,
        IAuditLogger auditLogger,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenStore = refreshTokenStore;
        _tokenService = tokenService;
        _mfaService = mfaService;
        _auditLogger = auditLogger;
        _passwordHasher = passwordHasher;
    }

    public async Task<(AuthResponseDto Response, int UserId)> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _userRepository.UsernameExistsAsync(dto.Username))
        {
            throw new ConflictException("El nombre de usuario ya esta en uso");
        }

        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            throw new ConflictException("El correo ya esta registrado");
        }

        var userRole = await _userRepository.GetRoleByNameAsync(RoleName.User)
            ?? throw new ApiException(500, "El rol de usuario por defecto no esta configurado");

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            Phone = dto.Phone,
            IdentificationNumber = dto.Identification,
            BirthDate = ParseBirthDate(dto.BirthDate),
            RoleId = userRole.Id,
            Status = UserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.Hash(dto.Password);

        await _userRepository.AddAsync(user);

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.UserCreated,
            Entity = "User",
            EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
            UserId = user.Id,
            Username = user.Username,
            After = new { user.Username, user.Email, Role = userRole.Name }
        });

        var response = new AuthResponseDto
        {
            Success = true,
            Message = "Registro exitoso. Ya puede iniciar sesion.",
            Data = new AuthDataDto()
        };

        return (response, user.Id);
    }

    public async Task<AuthResult> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user is null)
        {
            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.LoginFailed,
                Entity = "User",
                Username = dto.Username
            });
            throw new UnauthorizedApiException("Usuario o contrasena invalidos");
        }

        if (user.Status == UserStatus.INACTIVE)
        {
            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.AccountDisabled,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username
            });
            throw new ForbiddenException("La cuenta se encuentra inactiva. Contacte al administrador.");
        }

        if (user.LockedUntil.HasValue)
        {
            if (user.LockedUntil > DateTime.UtcNow)
            {
                _auditLogger.Enqueue(new AuditEvent
                {
                    EventType = AuditEventTypes.LoginFailed,
                    Entity = "User",
                    EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                    UserId = user.Id,
                    Username = user.Username
                });
                throw new ForbiddenException("La cuenta esta bloqueada temporalmente por demasiados intentos fallidos. Intente nuevamente mas tarde o contacte a un administrador.");
            }

            // El bloqueo temporal ya expiro: se desbloquea automaticamente antes de continuar.
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _userRepository.SaveChangesAsync();

            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.AccountUnlocked,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username,
                After = new { reason = "auto" }
            });
        }

        var verification = _passwordHasher.Verify(dto.Password, user.PasswordHash);
        if (verification == PasswordCheck.Failed)
        {
            user.FailedLoginAttempts++;

            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.LoginFailed,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username
            });

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _userRepository.SaveChangesAsync();

                _auditLogger.Enqueue(new AuditEvent
                {
                    EventType = AuditEventTypes.AccountLocked,
                    Entity = "User",
                    EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                    UserId = user.Id,
                    Username = user.Username,
                    After = new { lockedUntil = user.LockedUntil, failedAttempts = user.FailedLoginAttempts }
                });

                throw new ForbiddenException($"Ha superado el numero de intentos permitidos. La cuenta fue bloqueada por {LockoutMinutes} minutos.");
            }

            await _userRepository.SaveChangesAsync();
            throw new UnauthorizedApiException("Usuario o contrasena invalidos");
        }

        var needsSave = false;

        if (user.FailedLoginAttempts > 0)
        {
            user.FailedLoginAttempts = 0;
            needsSave = true;
        }

        // Migracion transparente: el hash heredado (PBKDF2) se regenera con BCrypt
        // usando la contrasena que el usuario acaba de escribir correctamente.
        if (verification == PasswordCheck.NeedsRehash)
        {
            user.PasswordHash = _passwordHasher.Hash(dto.Password);
            user.UpdatedAt = DateTime.UtcNow;
            needsSave = true;

            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.PasswordRehashed,
                Entity = "User",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                UserId = user.Id,
                Username = user.Username,
                After = new { algorithm = "bcrypt" }
            });
        }

        if (needsSave)
        {
            await _userRepository.SaveChangesAsync();
        }

        var mfaToken = await _mfaService.GenerateAsync(user.Id);

        var response = new AuthResponseDto
        {
            Success = true,
            RequiresMfa = true,
            Data = new AuthDataDto { MfaToken = mfaToken }
        };

        return new AuthResult(response, null);
    }

    public async Task<AuthResult> VerifyMfaAsync(MfaVerifyRequestDto dto)
    {
        var user = await _mfaService.VerifyAsync(dto.MfaToken, dto.Code);

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.MfaSuccess,
            Entity = "User",
            EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
            UserId = user.Id,
            Username = user.Username
        });
        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.LoginSuccess,
            Entity = "User",
            EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
            UserId = user.Id,
            Username = user.Username
        });

        var (response, refreshToken) = await IssueSessionAsync(user);
        return new AuthResult(response, refreshToken);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedApiException("No se encontro una sesion activa");
        }

        var hash = _tokenService.HashToken(refreshToken);
        var existing = await _refreshTokenStore.GetActiveByHashAsync(hash);

        if (existing is null)
        {
            throw new UnauthorizedApiException("La sesion expiro, inicie sesion nuevamente");
        }

        await _refreshTokenStore.RevokeAsync(existing);

        var (response, newRefreshToken) = await IssueSessionAsync(existing.User);
        return new AuthResult(response, newRefreshToken);
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = _tokenService.HashToken(refreshToken);
        var existing = await _refreshTokenStore.GetActiveByHashAsync(hash);

        if (existing is not null)
        {
            await _refreshTokenStore.RevokeAsync(existing);
            await _refreshTokenStore.SaveChangesAsync();

            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.Logout,
                Entity = "User",
                EntityId = existing.UserId.ToString(CultureInfo.InvariantCulture),
                UserId = existing.UserId,
                Username = existing.User.Username
            });
        }
    }

    private async Task<(AuthResponseDto Response, string RefreshToken)> IssueSessionAsync(User user)
    {
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshTokenRaw = _tokenService.GenerateRefreshToken();

        await _refreshTokenStore.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(refreshTokenRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        });

        var response = new AuthResponseDto
        {
            Success = true,
            Data = new AuthDataDto
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.Name
                }
            }
        };

        return (response, refreshTokenRaw);
    }

    private static DateOnly? ParseBirthDate(string? birthDate) =>
        DateOnly.TryParse(birthDate, out var parsed) ? parsed : null;
}
