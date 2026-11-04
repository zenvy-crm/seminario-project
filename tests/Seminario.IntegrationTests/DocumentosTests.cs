using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Seminario.Application.DTOs;
using Seminario.IntegrationTests.Infrastructure;

namespace Seminario.IntegrationTests;

[Collection(ApiCollection.Nombre)]
[Trait("Category", "Integration")]
public class DocumentosTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%documento de prueba\n");

    private readonly ApiFactory _api;

    public DocumentosTests(ApiFactory api) => _api = api;

    private static MultipartFormDataContent Archivo(byte[] contenido, string nombre, string contentType)
    {
        var form = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(archivo, "file", nombre);
        return form;
    }

    [Fact(DisplayName = "IT-DOC-01 Subir, listar, descargar y eliminar (soft delete) un documento propio")]
    public async Task CicloDeVida()
    {
        var username = await _api.RegistrarUsuarioAsync();
        var id = await _api.UserIdDe(username);
        var client = await _api.ClienteComo(username);

        using var form = Archivo(Pdf, "constancia.pdf", "application/pdf");
        var subida = await client.PostAsync($"/api/users/{id}/documents", form);
        Assert.Equal(HttpStatusCode.OK, subida.StatusCode);
        var doc = await subida.LeerAsync<DocumentDto>();
        Assert.Equal("constancia.pdf", doc.FileName);

        var lista = await client.GetFromJsonAsync<List<DocumentDto>>($"/api/users/{id}/documents");
        Assert.Single(lista!);

        var descarga = await client.GetAsync(doc.Url);
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Pdf, await descarga.Content.ReadAsByteArrayAsync());

        var borrado = await client.DeleteAsync($"/api/users/{id}/documents/{doc.Id}");
        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);

        Assert.Empty((await client.GetFromJsonAsync<List<DocumentDto>>($"/api/users/{id}/documents"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(doc.Url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/users/{id}/documents/{doc.Id}")).StatusCode);
    }

    [Fact(DisplayName = "IT-DOC-02 Un usuario no puede ver los documentos de otro (403); el admin si")]
    public async Task DocumentosAjenos_403()
    {
        var dueno = await _api.RegistrarUsuarioAsync();
        var idDueno = await _api.UserIdDe(dueno);
        var otro = await _api.ClienteComo(await _api.RegistrarUsuarioAsync());
        var admin = await _api.ClienteComo("admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await otro.GetAsync($"/api/users/{idDueno}/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/users/{idDueno}/documents")).StatusCode);
    }

    [Fact(DisplayName = "IT-DOC-03 [Regresion] Nombre de archivo con caracteres de control responde 422")]
    [Trait("Category", "Regression")]
    public async Task NombreConControl_422()
    {
        var username = await _api.RegistrarUsuarioAsync();
        var client = await _api.ClienteComo(username);
        // filename* (RFC 5987) permite enviar el caracter de control percent-encoded, como lo haria un cliente malicioso.
        using var form = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(Pdf);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        archivo.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileNameStar = "fact\u0001ura.pdf"
        };
        form.Add(archivo);

        var response = await client.PostAsync($"/api/users/{await _api.UserIdDe(username)}/documents", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "IT-DOC-04 Un PDF disfrazado de JPG se rechaza por su firma (422)")]
    public async Task ExtensionFalsa_422()
    {
        var username = await _api.RegistrarUsuarioAsync();
        var client = await _api.ClienteComo(username);
        using var form = Archivo(Pdf, "foto.jpg", "image/jpeg");

        var response = await client.PostAsync($"/api/users/{await _api.UserIdDe(username)}/documents", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
