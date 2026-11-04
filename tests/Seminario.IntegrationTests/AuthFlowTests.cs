using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Auth;
using Seminario.IntegrationTests.Infrastructure;

namespace Seminario.IntegrationTests;

[Collection(ApiCollection.Nombre)]
[Trait("Category", "Integration")]
public class AuthFlowTests
{
    private readonly ApiFactory _api;

    public AuthFlowTests(ApiFactory api) => _api = api;

    private async Task<(HttpClient Client, AuthResponseDto Sesion)> IniciarSesionAsync(string username, string password)
    {
        var client = _api.Cliente();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var desafio = await login.LeerAsync<AuthResponseDto>();
        Assert.True(desafio.RequiresMfa);

        var codigo = _api.CodigosMfa[await _api.UserIdDe(username)];
        var verificacion = await client.PostAsJsonAsync("/api/auth/mfa/verify", new { mfaToken = desafio.Data.MfaToken, code = codigo });
        Assert.Equal(HttpStatusCode.OK, verificacion.StatusCode);
        return (client, await verificacion.LeerAsync<AuthResponseDto>());
    }

    [Fact(DisplayName = "IT-AUTH-01 Flujo completo: login -> MFA -> perfil -> refresh -> logout")]
    public async Task FlujoCompleto()
    {
        var (client, sesion) = await IniciarSesionAsync("admin", "Admin#2026!");

        Assert.False(string.IsNullOrEmpty(sesion.Data.AccessToken));
        Assert.Equal("ADMIN", sesion.Data.User!.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Data.AccessToken);
        var perfil = await client.GetFromJsonAsync<UserDto>("/api/profile");
        Assert.Equal("admin", perfil!.Username);

        var refresh = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var renovada = await refresh.LeerAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(renovada.Data.AccessToken));

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var refreshTrasLogout = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshTrasLogout.StatusCode);
    }

    [Fact(DisplayName = "IT-AUTH-02 Contrasena incorrecta responde 401 con el formato de error uniforme")]
    public async Task ContrasenaIncorrecta_401()
    {
        var username = await _api.RegistrarUsuarioAsync();

        var response = await _api.Cliente().PostAsJsonAsync("/api/auth/login", new { username, password = "Incorrecta#1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.LeerAsync<ApiErrorResponse>();
        Assert.False(error.Success);
        Assert.Equal(401, error.StatusCode);
    }

    [Fact(DisplayName = "IT-AUTH-03 [Regresion] 3 intentos fallidos bloquean la cuenta y el admin puede desbloquearla")]
    [Trait("Category", "Regression")]
    public async Task Bloqueo_YDesbloqueoManual()
    {
        var username = await _api.RegistrarUsuarioAsync("Correcta#2026");
        var client = _api.Cliente();

        for (var i = 0; i < 2; i++)
        {
            var fallo = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "mala-" + i });
            Assert.Equal(HttpStatusCode.Unauthorized, fallo.StatusCode);
        }

        var tercero = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "mala-3" });
        Assert.Equal(HttpStatusCode.Forbidden, tercero.StatusCode);

        var conCorrecta = await _api.Cliente().PostAsJsonAsync("/api/auth/login", new { username, password = "Correcta#2026" });
        Assert.Equal(HttpStatusCode.Forbidden, conCorrecta.StatusCode);

        var admin = await _api.ClienteComo("admin");
        var id = await _api.UserIdDe(username);
        var desbloqueo = await admin.PatchAsync($"/api/users/{id}/unlock", null);
        Assert.Equal(HttpStatusCode.OK, desbloqueo.StatusCode);
        Assert.False((await desbloqueo.LeerAsync<UserDto>()).IsLocked);

        var trasDesbloqueo = await _api.Cliente().PostAsJsonAsync("/api/auth/login", new { username, password = "Correcta#2026" });
        Assert.Equal(HttpStatusCode.OK, trasDesbloqueo.StatusCode);
    }

    [Fact(DisplayName = "IT-AUTH-04 Codigo MFA incorrecto responde 401")]
    public async Task MfaIncorrecto_401()
    {
        var username = await _api.RegistrarUsuarioAsync();
        var client = _api.Cliente();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "Segura#2026" });
        var desafio = await login.LeerAsync<AuthResponseDto>();
        var correcto = _api.CodigosMfa[await _api.UserIdDe(username)];
        var incorrecto = correcto == "000000" ? "111111" : "000000";

        var response = await client.PostAsJsonAsync("/api/auth/mfa/verify", new { mfaToken = desafio.Data.MfaToken, code = incorrecto });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "IT-AUTH-05 [Regresion] Rate limiting: el 6.o login por minuto desde la misma IP responde 429")]
    [Trait("Category", "Regression")]
    public async Task RateLimiting_429()
    {
        var client = _api.Cliente();
        HttpResponseMessage? ultima = null;

        for (var i = 0; i < 6; i++)
        {
            ultima = await client.PostAsJsonAsync("/api/auth/login", new { username = "no-existe", password = "x" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
        Assert.True(ultima.Headers.Contains("Retry-After"));
    }

    [Fact(DisplayName = "IT-AUTH-06 Registro con datos invalidos responde 422 con la lista de errores")]
    public async Task Registro_Invalido_422()
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("A"), "FirstName" },
            { new StringContent("B"), "LastName" },
            { new StringContent("usuario con espacios"), "Username" },
            { new StringContent("no-es-correo"), "Email" },
            { new StringContent("corta"), "Password" },
            { new StringContent("distinta"), "ConfirmPassword" }
        };

        var response = await _api.Cliente().PostAsync("/api/auth/register", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.LeerAsync<ApiErrorResponse>();
        Assert.True(error.Errors.Count >= 3);
    }

    [Fact(DisplayName = "IT-AUTH-07 [Regresion] Registro con documento invalido no crea el usuario")]
    [Trait("Category", "Regression")]
    public async Task Registro_DocumentoInvalido_NoCreaUsuario()
    {
        var username = "it_doc_" + Guid.NewGuid().ToString("N")[..8];
        using var form = FormularioRegistro(username);
        var archivo = new ByteArrayContent(Encoding.UTF8.GetBytes("esto no es un pdf"));
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(archivo, "document", "dpi.pdf");

        var response = await _api.Cliente().PostAsync("/api/auth/register", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var login = await _api.Cliente().PostAsJsonAsync("/api/auth/login", new { username, password = "Segura#2026" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact(DisplayName = "IT-AUTH-08 Registro con un PDF valido guarda el usuario y su documento")]
    public async Task Registro_ConPdf_GuardaDocumento()
    {
        var username = "it_pdf_" + Guid.NewGuid().ToString("N")[..8];
        using var form = FormularioRegistro(username);
        var archivo = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.7\n%prueba\n"));
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(archivo, "document", "dpi.pdf");

        var response = await _api.Cliente().PostAsync("/api/auth/register", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var admin = await _api.ClienteComo("admin");
        var docs = await admin.GetFromJsonAsync<List<DocumentDto>>($"/api/users/{await _api.UserIdDe(username)}/documents");
        Assert.Single(docs!);
    }

    [Fact(DisplayName = "IT-AUTH-09 Registro con username existente responde 409")]
    public async Task Registro_Duplicado_409()
    {
        using var form = FormularioRegistro("admin");

        var response = await _api.Cliente().PostAsync("/api/auth/register", form);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "IT-SEC-01 Sin token o con token alterado la API responde 401")]
    public async Task SinToken_401()
    {
        var sinToken = await _api.Cliente().GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal(401, (await sinToken.LeerAsync<ApiErrorResponse>()).StatusCode);

        var alterado = await _api.ClienteComo("estandar");
        var token = alterado.DefaultRequestHeaders.Authorization!.Parameter!;
        alterado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");
        Assert.Equal(HttpStatusCode.Unauthorized, (await alterado.GetAsync("/api/profile")).StatusCode);
    }

    [Fact(DisplayName = "IT-SEC-02 Un usuario Estandar no puede administrar usuarios (403)")]
    public async Task Estandar_NoAdministraUsuarios_403()
    {
        var client = await _api.ClienteComo("estandar");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static MultipartFormDataContent FormularioRegistro(string username) => new()
    {
        { new StringContent("Prueba"), "FirstName" },
        { new StringContent("Documento"), "LastName" },
        { new StringContent(username), "Username" },
        { new StringContent($"{username}@test.local"), "Email" },
        { new StringContent("Segura#2026"), "Password" },
        { new StringContent("Segura#2026"), "ConfirmPassword" }
    };
}
