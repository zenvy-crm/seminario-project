namespace Seminario.Api;

/// <summary>
/// Nombres de las politicas de rate limiting registradas en Program.cs.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Endpoints de autenticacion: 5 peticiones por minuto por direccion IP.</summary>
    public const string Auth = "auth";
}
