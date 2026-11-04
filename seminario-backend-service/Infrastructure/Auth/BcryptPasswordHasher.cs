using Microsoft.AspNetCore.Identity;
using Seminario.Application.Interfaces;

namespace Seminario.Infrastructure.Auth;

/// <summary>
/// Hashing de contrasenas con BCrypt (salt de 16 bytes generado y embebido por la libreria).
/// Los hashes heredados de PBKDF2 (ASP.NET Core Identity) se siguen verificando y se marcan
/// para re-hash, de modo que la migracion ocurre de forma transparente en el proximo login.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>Costo 12 => 2^12 rondas, ~250 ms por hash en hardware actual.</summary>
    private const int WorkFactor = 12;

    private static readonly PasswordHasher<object> LegacyHasher = new();
    private static readonly object LegacyUser = new();

    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.EnhancedHashPassword(plainPassword, WorkFactor);

    public PasswordCheck Verify(string plainPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return PasswordCheck.Failed;
        }

        // Los hashes de BCrypt siempre inician con $2a$ / $2b$ / $2y$.
        if (!storedHash.StartsWith("$2", StringComparison.Ordinal))
        {
            return VerifyLegacy(plainPassword, storedHash);
        }

        if (!BCrypt.Net.BCrypt.EnhancedVerify(plainPassword, storedHash))
        {
            return PasswordCheck.Failed;
        }

        // Permite subir el WorkFactor en el futuro sin tocar la base de datos manualmente.
        return BCrypt.Net.BCrypt.PasswordNeedsRehash(storedHash, WorkFactor)
            ? PasswordCheck.NeedsRehash
            : PasswordCheck.Ok;
    }

    private static PasswordCheck VerifyLegacy(string plainPassword, string storedHash)
    {
        PasswordVerificationResult result;
        try
        {
            result = LegacyHasher.VerifyHashedPassword(LegacyUser, storedHash, plainPassword);
        }
        catch (FormatException)
        {
            // El hash almacenado no es PBKDF2 valido: se trata como fallo, nunca como exito.
            return PasswordCheck.Failed;
        }

        return result == PasswordVerificationResult.Failed
            ? PasswordCheck.Failed
            : PasswordCheck.NeedsRehash;
    }
}
