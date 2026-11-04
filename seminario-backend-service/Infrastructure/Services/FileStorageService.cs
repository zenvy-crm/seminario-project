using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Seminario.Application.Common;
using Seminario.Application.Interfaces;

namespace Seminario.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const int MaxFileNameLength = 200;

    private static readonly Dictionary<string, string> AllowedExtensionsToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg"
    };

    private readonly string _uploadsRoot;

    public FileStorageService(IWebHostEnvironment env)
    {
        _uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");
    }

    public async Task ValidateOnlyAsync(IFormFile file)
    {
        await ValidateAsync(file);
    }

    public async Task<StoredFile> ValidateAndStoreAsync(IFormFile file, int userId)
    {
        var (originalName, extension, expectedContentType, content) = await ValidateAsync(file);

        var userFolder = Path.Combine(_uploadsRoot, userId.ToString());
        Directory.CreateDirectory(userFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(userFolder, storedFileName);
        await File.WriteAllBytesAsync(fullPath, content);

        var relativePath = Path.Combine(userId.ToString(), storedFileName).Replace('\\', '/');

        return new StoredFile(originalName, storedFileName, expectedContentType, content.LongLength, relativePath);
    }

    private static async Task<(string OriginalName, string Extension, string ExpectedContentType, byte[] Content)> ValidateAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ValidationApiException("El archivo esta vacio", new List<string> { "file" });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ValidationApiException("El archivo supera el tamano maximo permitido de 5MB", new List<string> { "file" });
        }

        var originalName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(originalName) || originalName.Any(c => char.IsControl(c)))
        {
            throw new ValidationApiException("El nombre del archivo no es valido", new List<string> { "file" });
        }

        if (originalName.Length > MaxFileNameLength)
        {
            throw new ValidationApiException($"El nombre del archivo no puede superar los {MaxFileNameLength} caracteres", new List<string> { "file" });
        }

        var extension = Path.GetExtension(originalName);

        if (string.IsNullOrEmpty(extension) || !AllowedExtensionsToContentType.TryGetValue(extension, out var expectedContentType))
        {
            throw new ValidationApiException("Solo se permiten archivos PDF, JPG o JPEG", new List<string> { "file" });
        }

        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationApiException("El tipo de archivo no coincide con la extension declarada", new List<string> { "file" });
        }

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var content = memoryStream.ToArray();

        if (!MatchesFileSignature(content, extension))
        {
            throw new ValidationApiException("El contenido del archivo no corresponde con su extension", new List<string> { "file" });
        }

        return (originalName, extension, expectedContentType, content);
    }

    public async Task<byte[]> ReadAsync(string storagePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_uploadsRoot, storagePath));

        if (!fullPath.StartsWith(Path.GetFullPath(_uploadsRoot), StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            throw new NotFoundException("El archivo no existe");
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

    /// <summary>
    /// Compensacion de una transaccion fallida: elimina el archivo que ya se habia
    /// escrito en disco. No lanza si el archivo no existe ni si la ruta es invalida,
    /// porque se invoca desde bloques catch donde ya hay una excepcion en curso.
    /// </summary>
    public void Delete(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_uploadsRoot, storagePath));

        // Misma barrera contra path traversal que en ReadAsync.
        if (!fullPath.StartsWith(Path.GetFullPath(_uploadsRoot), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            File.Delete(fullPath);
        }
        catch (IOException)
        {
            // El archivo quedaria huerfano, pero no se puede escalar el fallo:
            // se perderia la excepcion original de la transaccion.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }



    private static bool MatchesFileSignature(byte[] content, string extension)
    {
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return content.Length >= 4 && content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46;
        }

        // JPEG / JPG
        return content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;
    }
}


