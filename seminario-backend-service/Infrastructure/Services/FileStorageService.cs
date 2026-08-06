using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Seminario.Application.Common;
using Seminario.Application.Interfaces;

namespace Seminario.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

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

    public async Task<StoredFile> ValidateAndStoreAsync(IFormFile file, int userId)
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

        var userFolder = Path.Combine(_uploadsRoot, userId.ToString());
        Directory.CreateDirectory(userFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(userFolder, storedFileName);
        await File.WriteAllBytesAsync(fullPath, content);

        var relativePath = Path.Combine(userId.ToString(), storedFileName).Replace('\\', '/');

        return new StoredFile(originalName, storedFileName, expectedContentType, content.LongLength, relativePath);
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
