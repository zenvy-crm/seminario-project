namespace Seminario.Application.Common;

/// <summary>
/// Usuario autenticado tal como lo describe el JWT. Lo arma el controlador y lo recibe el
/// servicio, que asi aplica las reglas de visibilidad por rol sin depender del HttpContext.
/// </summary>
/// <param name="Rol">Nombre del rol (USER / INTERMEDIO / ADMIN).</param>
/// <param name="Nivel">1 Estandar, 2 Intermedio, 3 Administrador.</param>
public sealed record UsuarioSesion(int UserId, string Username, string Rol, int Nivel)
{
    /// <summary>El rol Estandar (USER) nunca ve ordenes ANULADA.</summary>
    public bool EsEstandar => Nivel <= Domain.Enums.RoleName.NivelEstandar;
}
