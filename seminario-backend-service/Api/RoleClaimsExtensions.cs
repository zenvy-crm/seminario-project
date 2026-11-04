using System.Security.Claims;
using Seminario.Application.Common;
using Seminario.Domain.Enums;

namespace Seminario.Api;

/// <summary>Lectura del rol, el nivel y el usuario de la sesion a partir de los claims del JWT.</summary>
public static class RoleClaimsExtensions
{
    /// <summary>Nombre del claim con el nivel de acceso (1 Estandar, 2 Intermedio, 3 Administrador).</summary>
    public const string NivelClaim = "nivel";

    public static string GetRoleName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role") ?? string.Empty;

    /// <summary>
    /// Nivel del claim "nivel"; si el token es anterior a la Fase 2 y no lo trae, se deriva del rol.
    /// El claim nunca puede elevar el nivel por encima del que corresponde al rol.
    /// </summary>
    public static int GetNivel(this ClaimsPrincipal user)
    {
        var nivelRol = RoleName.NivelDe(user.GetRoleName());
        return int.TryParse(user.FindFirstValue(NivelClaim), out var nivel) ? Math.Min(nivel, nivelRol) : nivelRol;
    }

    public static string GetUsername(this ClaimsPrincipal user) =>
        user.FindFirstValue("username") ?? user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    /// <summary>Datos de la sesion que los servicios necesitan, sin que tengan que leer el HttpContext.</summary>
    public static UsuarioSesion ToUsuarioSesion(this ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        if (userId == 0)
        {
            throw new ForbiddenException("El token no contiene un usuario valido");
        }

        return new UsuarioSesion(userId, user.GetUsername(), user.GetRoleName(), user.GetNivel());
    }
}
