using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Seminario.Application.Interfaces;
using Seminario.Domain.Enums;
using Seminario.Infrastructure.Auth;
using Seminario.UnitTests.Helpers;

namespace Seminario.UnitTests.Infrastructure;

[Trait("Category", "Unit")]
public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact(DisplayName = "UT-PWD-01 Hash BCrypt verifica la contrasena correcta y rechaza la incorrecta")]
    public void HashYVerificar()
    {
        var hash = _sut.Hash("Segura#2026");

        Assert.StartsWith("$2", hash);
        Assert.DoesNotContain("Segura#2026", hash);
        Assert.Equal(PasswordCheck.Ok, _sut.Verify("Segura#2026", hash));
        Assert.Equal(PasswordCheck.Failed, _sut.Verify("otra", hash));
    }

    [Theory(DisplayName = "UT-PWD-02 Hash vacio o PBKDF2 corrupto nunca se acepta")]
    [InlineData("")]
    [InlineData("no-es-un-hash-valido")]
    public void HashInvalido_Falla(string hash)
    {
        Assert.Equal(PasswordCheck.Failed, _sut.Verify("x", hash));
    }

    [Fact(DisplayName = "UT-PWD-03 Hash heredado PBKDF2 valido se acepta y se marca para re-hash")]
    public void HashHeredado_NeedsRehash()
    {
        var legacy = new PasswordHasher<object>().HashPassword(new object(), "Vieja#123");

        Assert.Equal(PasswordCheck.NeedsRehash, _sut.Verify("Vieja#123", legacy));
        Assert.Equal(PasswordCheck.Failed, _sut.Verify("otra", legacy));
    }

    [Fact(DisplayName = "UT-PWD-04 Hash BCrypt con costo menor al vigente se marca para re-hash")]
    public void CostoBajo_NeedsRehash()
    {
        var hashCosto4 = BCrypt.Net.BCrypt.EnhancedHashPassword("Segura#2026", 4);

        Assert.Equal(PasswordCheck.NeedsRehash, _sut.Verify("Segura#2026", hashCosto4));
    }
}

[Trait("Category", "Unit")]
public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "clave-de-pruebas-unitarias-con-mas-de-32-bytes!!",
        Issuer = "seminario-tests",
        Audience = "seminario-tests-clients",
        ExpirationMinutes = 15,
        RefreshTokenExpirationDays = 3
    };

    private readonly TokenService _sut = new(Options.Create(Settings));

    [Fact(DisplayName = "UT-TKN-01 Access token incluye sub, username, rol y nivel RBAC")]
    public void AccessToken_IncluyeClaims()
    {
        var user = TestData.Usuario(id: 15, username: "inter", rol: RoleName.Intermedio);

        var (token, expira) = _sut.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("seminario-tests", jwt.Issuer);
        Assert.Contains("seminario-tests-clients", jwt.Audiences);
        Assert.Equal("15", jwt.Subject);
        Assert.Equal("inter", jwt.Claims.First(c => c.Type == "username").Value);
        Assert.Equal(RoleName.Intermedio, jwt.Claims.First(c => c.Type is "role" or System.Security.Claims.ClaimTypes.Role).Value);
        Assert.Equal("2", jwt.Claims.First(c => c.Type == "nivel").Value);
        Assert.InRange(expira, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
        Assert.Equal(3, _sut.RefreshTokenExpirationDays);
    }

    [Fact(DisplayName = "UT-TKN-02 Refresh token es aleatorio y seguro para URL")]
    public void RefreshToken_AleatorioYUrlSafe()
    {
        var a = _sut.GenerateRefreshToken();
        var b = _sut.GenerateRefreshToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 80);
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.DoesNotContain('=', a);
    }

    [Fact(DisplayName = "UT-TKN-03 HashToken es SHA-256 hexadecimal y determinista")]
    public void HashToken_Determinista()
    {
        var h1 = _sut.HashToken("abc");

        Assert.Equal(h1, _sut.HashToken("abc"));
        Assert.NotEqual(h1, _sut.HashToken("abd"));
        Assert.Equal(64, h1.Length);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", h1);
    }
}
