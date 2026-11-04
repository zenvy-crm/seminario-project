using Seminario.Domain.Enums;

namespace Seminario.Api;

/// <summary>
/// Politicas RBAC de la Fase 2. Son acumulativas: cada nivel incluye a los superiores, de modo
/// que el Administrador puede todo lo que puede el Intermedio y este todo lo del Estandar.
/// Se evaluan contra el claim de rol del JWT (USER / INTERMEDIO / ADMIN).
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Nivel 1: USER, INTERMEDIO o ADMIN. Crear y consultar ordenes.</summary>
    public const string NivelEstandar = "NivelEstandar";

    /// <summary>Nivel 2: INTERMEDIO o ADMIN. Anular (soft delete) lineas del detalle.</summary>
    public const string NivelIntermedio = "NivelIntermedio";

    /// <summary>Nivel 3: solo ADMIN. Anular (soft delete) la orden completa.</summary>
    public const string NivelAdministrador = "NivelAdministrador";

    public static IServiceCollection AddRbacPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(NivelEstandar, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleName.User, RoleName.Intermedio, RoleName.Admin));

            options.AddPolicy(NivelIntermedio, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleName.Intermedio, RoleName.Admin));

            options.AddPolicy(NivelAdministrador, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleName.Admin));
        });

        return services;
    }
}
