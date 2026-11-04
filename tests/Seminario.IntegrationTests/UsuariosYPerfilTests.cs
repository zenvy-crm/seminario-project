using System.Net;
using System.Net.Http.Json;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.IntegrationTests.Infrastructure;

namespace Seminario.IntegrationTests;

[Collection(ApiCollection.Nombre)]
[Trait("Category", "Integration")]
public class UsuariosYPerfilTests
{
    private readonly ApiFactory _api;

    public UsuariosYPerfilTests(ApiFactory api) => _api = api;

    [Fact(DisplayName = "IT-USR-01 CRUD de usuarios por el administrador (crear, consultar, editar, listar)")]
    public async Task CrudUsuarios()
    {
        var admin = await _api.ClienteComo("admin");
        var username = "it_crud_" + Guid.NewGuid().ToString("N")[..8];

        var creado = await admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Carla",
            lastName = "Mendez",
            username,
            email = $"{username}@test.local",
            password = "Segura#2026",
            role = "intermedio"
        });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.NotNull(creado.Headers.Location);
        var dto = await creado.LeerAsync<UserDto>();
        Assert.Equal("INTERMEDIO", dto.Role);

        var consultado = await admin.GetFromJsonAsync<UserDto>($"/api/users/{dto.Id}");
        Assert.Equal(username, consultado!.Username);

        var editado = await admin.PutAsJsonAsync($"/api/users/{dto.Id}", new { firstName = "Carla Maria", phone = "5555-1234" });
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        Assert.Equal("Carla Maria", (await editado.LeerAsync<UserDto>()).FirstName);

        var listado = await admin.GetFromJsonAsync<PagedResult<UserDto>>($"/api/users?search={username}&sortBy=username&sortDir=desc");
        Assert.Equal(1, listado!.Total);
    }

    [Fact(DisplayName = "IT-USR-02 Desactivar un usuario le impide iniciar sesion (403)")]
    public async Task Desactivar_ImpideLogin()
    {
        var username = await _api.RegistrarUsuarioAsync();
        var admin = await _api.ClienteComo("admin");
        var id = await _api.UserIdDe(username);

        var estado = await admin.PatchAsJsonAsync($"/api/users/{id}/status", new { status = "INACTIVE" });
        Assert.Equal("INACTIVE", (await estado.LeerAsync<UserDto>()).Status);

        var login = await _api.Cliente().PostAsJsonAsync("/api/auth/login", new { username, password = "Segura#2026" });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [Fact(DisplayName = "IT-USR-03 Cambiar el rol de un usuario se refleja en su DTO")]
    public async Task CambiarRol()
    {
        var username = await _api.RegistrarUsuarioAsync();
        var admin = await _api.ClienteComo("admin");

        var response = await admin.PatchAsJsonAsync($"/api/users/{await _api.UserIdDe(username)}/role", new { role = "INTERMEDIO" });

        Assert.Equal("INTERMEDIO", (await response.LeerAsync<UserDto>()).Role);
    }

    [Fact(DisplayName = "IT-USR-04 Usuario inexistente responde 404 y creacion invalida responde 422")]
    public async Task Errores_404Y422()
    {
        var admin = await _api.ClienteComo("admin");

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/users/999999")).StatusCode);

        var invalido = await admin.PostAsJsonAsync("/api/users", new { firstName = "X", lastName = "Y", username = "sin_correo", password = "123" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
    }

    [Fact(DisplayName = "IT-PRF-01 El usuario edita su perfil y cambia su contrasena")]
    public async Task Perfil_EditarYCambiarContrasena()
    {
        var username = await _api.RegistrarUsuarioAsync("Inicial#2026");
        var client = await _api.ClienteComo(username);

        var editado = await client.PutAsJsonAsync("/api/profile", new { firstName = "Nombre Nuevo", phone = "3333-0000" });
        Assert.Equal("Nombre Nuevo", (await editado.LeerAsync<UserDto>()).FirstName);

        var mala = await client.PostAsJsonAsync("/api/profile/change-password", new { currentPassword = "otra", newPassword = "Nueva#20260" });
        Assert.Equal(HttpStatusCode.Unauthorized, mala.StatusCode);

        var buena = await client.PostAsJsonAsync("/api/profile/change-password", new { currentPassword = "Inicial#2026", newPassword = "Nueva#20260" });
        Assert.Equal(HttpStatusCode.OK, buena.StatusCode);

        var login = await _api.Cliente().PostAsJsonAsync("/api/auth/login", new { username, password = "Nueva#20260" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
