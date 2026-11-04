namespace Seminario.Application.Interfaces;

/// <summary>
/// Resultado de verificar una contrasena contra el hash almacenado.
/// </summary>
public enum PasswordCheck
{
    /// <summary>La contrasena no coincide.</summary>
    Failed,

    /// <summary>La contrasena coincide y el hash usa el algoritmo y costo vigentes.</summary>
    Ok,

    /// <summary>La contrasena coincide pero el hash es antiguo: debe regenerarse.</summary>
    NeedsRehash
}

public interface IPasswordHasher
{
    string Hash(string plainPassword);

    PasswordCheck Verify(string plainPassword, string storedHash);
}
