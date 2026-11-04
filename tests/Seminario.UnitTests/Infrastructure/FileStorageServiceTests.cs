using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Seminario.Application.Common;
using Seminario.Infrastructure.Services;

namespace Seminario.UnitTests.Infrastructure;

[Trait("Category", "Unit")]
public sealed class FileStorageServiceTests : IDisposable
{
    private static readonly byte[] FirmaPdf = Encoding.ASCII.GetBytes("%PDF-1.7\n% prueba\n");
    private static readonly byte[] FirmaJpg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };

    private readonly string _raiz;
    private readonly FileStorageService _sut;

    public FileStorageServiceTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "sqa-uploads-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_raiz);
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(_raiz);
        _sut = new FileStorageService(env.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    private static FormFile Archivo(byte[] contenido, string nombre, string contentType)
    {
        var stream = new MemoryStream(contenido);
        return new FormFile(stream, 0, contenido.Length, "file", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact(DisplayName = "UT-FS-01 Archivo vacio responde 422")]
    public async Task Validar_ArchivoVacio_Lanza422()
    {
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(Array.Empty<byte>(), "a.pdf", "application/pdf")));
        Assert.Contains("vacio", ex.Message);
    }

    [Fact(DisplayName = "UT-FS-02 Archivo mayor a 5 MB responde 422")]
    public async Task Validar_ArchivoGrande_Lanza422()
    {
        var grande = new byte[5 * 1024 * 1024 + 1];
        FirmaPdf.CopyTo(grande, 0);

        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(grande, "a.pdf", "application/pdf")));
        Assert.Contains("5MB", ex.Message);
    }

    [Fact(DisplayName = "UT-FS-03 [Regresion] Nombre con caracteres de control responde 422")]
    [Trait("Category", "Regression")]
    public async Task Validar_NombreConControl_Lanza422()
    {
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(FirmaPdf, "fact\u0007ura.pdf", "application/pdf")));
        Assert.Contains("nombre", ex.Message);
    }

    [Fact(DisplayName = "UT-FS-04 [Regresion] Nombre de mas de 200 caracteres responde 422")]
    [Trait("Category", "Regression")]
    public async Task Validar_NombreLargo_Lanza422()
    {
        var nombre = new string('a', 201) + ".pdf";
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(FirmaPdf, nombre, "application/pdf")));
        Assert.Contains("200", ex.Message);
    }

    [Theory(DisplayName = "UT-FS-05 Extension no permitida responde 422")]
    [InlineData("script.exe", "application/octet-stream")]
    [InlineData("foto.png", "image/png")]
    [InlineData("sin-extension", "application/pdf")]
    public async Task Validar_ExtensionNoPermitida_Lanza422(string nombre, string contentType)
    {
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(FirmaPdf, nombre, contentType)));
        Assert.Contains("PDF, JPG o JPEG", ex.Message);
    }

    [Fact(DisplayName = "UT-FS-06 Content-Type distinto a la extension responde 422")]
    public async Task Validar_ContentTypeNoCoincide_Lanza422()
    {
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(FirmaPdf, "doc.pdf", "image/jpeg")));
        Assert.Contains("no coincide", ex.Message);
    }

    [Fact(DisplayName = "UT-FS-07 Contenido que no corresponde a la extension (firma) responde 422")]
    public async Task Validar_FirmaFalsa_Lanza422()
    {
        var ex = await Assert.ThrowsAsync<ValidationApiException>(() =>
            _sut.ValidateOnlyAsync(Archivo(Encoding.ASCII.GetBytes("MZ ejecutable"), "doc.pdf", "application/pdf")));
        Assert.Contains("contenido", ex.Message);
    }

    [Fact(DisplayName = "UT-FS-08 PDF valido se guarda en la carpeta del usuario con nombre aleatorio")]
    public async Task Guardar_PdfValido_SeEscribeEnDisco()
    {
        var guardado = await _sut.ValidateAndStoreAsync(Archivo(FirmaPdf, "Contrato.PDF", "application/pdf"), 42);

        Assert.Equal("Contrato.PDF", guardado.OriginalFileName);
        Assert.Equal("application/pdf", guardado.ContentType);
        Assert.EndsWith(".pdf", guardado.StoredFileName);
        Assert.StartsWith("42/", guardado.StoragePath);
        Assert.Equal(FirmaPdf.Length, guardado.Size);
        Assert.True(File.Exists(Path.Combine(_raiz, "uploads", "42", guardado.StoredFileName)));
    }

    [Fact(DisplayName = "UT-FS-09 JPEG valido se acepta")]
    public async Task Guardar_JpegValido_SeAcepta()
    {
        var guardado = await _sut.ValidateAndStoreAsync(Archivo(FirmaJpg, "foto.jpeg", "image/jpeg"), 1);

        Assert.Equal("image/jpeg", guardado.ContentType);
        var leido = await _sut.ReadAsync(guardado.StoragePath);
        Assert.Equal(FirmaJpg, leido);
    }

    [Fact(DisplayName = "UT-FS-10 Lectura con path traversal (../) responde 404")]
    public async Task Leer_PathTraversal_Lanza404()
    {
        await File.WriteAllTextAsync(Path.Combine(_raiz, "secreto.txt"), "no debe leerse");

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ReadAsync("../secreto.txt"));
    }

    [Fact(DisplayName = "UT-FS-11 Lectura de un archivo inexistente responde 404")]
    public async Task Leer_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ReadAsync("1/no-existe.pdf"));
    }

    [Fact(DisplayName = "UT-FS-12 Delete (compensacion) borra el archivo y es idempotente")]
    public async Task Borrar_EsIdempotente()
    {
        var guardado = await _sut.ValidateAndStoreAsync(Archivo(FirmaPdf, "a.pdf", "application/pdf"), 3);
        var ruta = Path.Combine(_raiz, "uploads", "3", guardado.StoredFileName);

        _sut.Delete(guardado.StoragePath);
        _sut.Delete(guardado.StoragePath);
        _sut.Delete("");

        Assert.False(File.Exists(ruta));
    }

    [Fact(DisplayName = "UT-FS-13 Delete ignora rutas fuera de la carpeta de uploads")]
    public void Borrar_PathTraversal_NoBorra()
    {
        var externo = Path.Combine(_raiz, "externo.txt");
        File.WriteAllText(externo, "x");

        _sut.Delete("../externo.txt");

        Assert.True(File.Exists(externo));
    }
}
