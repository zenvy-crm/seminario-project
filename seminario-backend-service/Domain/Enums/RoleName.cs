namespace Seminario.Domain.Enums;

/// <summary>
/// Nombres de rol tal como viajan en el claim de rol del JWT. El frontend tiene ADMIN y USER
/// quemados, por eso no se renombran. Mapeo de la Fase 2:
/// USER = Estandar (nivel 1), INTERMEDIO = Intermedio (nivel 2), ADMIN = Administrador (nivel 3).
/// </summary>
public static class RoleName
{
    public const string Admin = "ADMIN";
    public const string Intermedio = "INTERMEDIO";
    public const string User = "USER";

    public const int NivelEstandar = 1;
    public const int NivelIntermedio = 2;
    public const int NivelAdministrador = 3;

    /// <summary>Nivel de acceso (1/2/3) del rol. Un rol desconocido no tiene nivel (0).</summary>
    public static int NivelDe(string? rol) => rol?.ToUpperInvariant() switch
    {
        User => NivelEstandar,
        Intermedio => NivelIntermedio,
        Admin => NivelAdministrador,
        _ => 0
    };

    /// <summary>Nombre del rol segun la consigna de la Fase 2 (campo rol_auth de la auditoria).</summary>
    public static string RolAuthDe(string? rol) => rol?.ToUpperInvariant() switch
    {
        User => "Estandar",
        Intermedio => "Intermedio",
        Admin => "Administrador",
        _ => rol ?? string.Empty
    };
}
