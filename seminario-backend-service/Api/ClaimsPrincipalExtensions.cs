using System.Security.Claims;

namespace Seminario.Api;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? id : 0;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Domain.Enums.RoleName.Admin);
}
