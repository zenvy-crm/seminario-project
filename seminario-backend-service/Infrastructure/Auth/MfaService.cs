using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace Seminario.Infrastructure.Auth;

public class MfaService : IMfaService
{
    private const int CodeExpirationMinutes = 5;

    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<MfaService> _logger;
    private readonly PasswordHasher<MfaCode> _hasher = new();

    public MfaService(AppDbContext db, IAuditLogger auditLogger, ILogger<MfaService> logger)
    {
        _db = db;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(int userId)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var mfaToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var mfaCode = new MfaCode
        {
            UserId = userId,
            MfaToken = mfaToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes),
            CreatedAt = DateTime.UtcNow
        };
        mfaCode.CodeHash = _hasher.HashPassword(mfaCode, code);

        _db.MfaCodes.Add(mfaCode);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Codigo MFA para el usuario {UserId}: {Code} (valido {Minutes} min)", userId, code, CodeExpirationMinutes);

        return mfaToken;
    }

    public async Task<User> VerifyAsync(string mfaToken, string code)
    {
        var mfaCode = await _db.MfaCodes
            .Include(m => m.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(m => m.MfaToken == mfaToken);

        if (mfaCode is null)
        {
            LogMfaFailed(null, null);
            throw new UnauthorizedApiException("El codigo de verificacion es invalido o expiro");
        }

        if (mfaCode.IsUsed || mfaCode.IsExpired)
        {
            LogMfaFailed(mfaCode.UserId, mfaCode.User.Username);
            throw new UnauthorizedApiException("El codigo de verificacion es invalido o expiro");
        }

        if (mfaCode.Attempts >= MfaCode.MaxAttempts)
        {
            LogMfaFailed(mfaCode.UserId, mfaCode.User.Username);
            throw new UnauthorizedApiException("Se supero el numero maximo de intentos, solicite un nuevo codigo");
        }

        var result = _hasher.VerifyHashedPassword(mfaCode, mfaCode.CodeHash, code);
        if (result == PasswordVerificationResult.Failed)
        {
            mfaCode.Attempts++;
            await _db.SaveChangesAsync();

            LogMfaFailed(mfaCode.UserId, mfaCode.User.Username);
            throw new UnauthorizedApiException("El codigo de verificacion es incorrecto");
        }

        mfaCode.IsUsed = true;
        await _db.SaveChangesAsync();

        return mfaCode.User;
    }

    private void LogMfaFailed(int? userId, string? username)
    {
        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.MfaFailed,
            Entity = "User",
            EntityId = userId?.ToString(),
            UserId = userId,
            Username = username
        });
    }
}
