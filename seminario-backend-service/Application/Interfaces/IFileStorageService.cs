using Microsoft.AspNetCore.Http;

namespace Seminario.Application.Interfaces;

public record StoredFile(string OriginalFileName, string StoredFileName, string ContentType, long Size, string StoragePath);

public interface IFileStorageService
{
    Task ValidateOnlyAsync(IFormFile file);
    Task<StoredFile> ValidateAndStoreAsync(IFormFile file, int userId);
    Task<byte[]> ReadAsync(string storagePath);

    /// <summary>
    /// Borra un archivo ya almacenado. Se usa como compensacion cuando la transaccion
    /// de base de datos hace rollback y el archivo en disco quedaria huerfano.
    /// Es idempotente: si el archivo no existe, no hace nada.
    /// </summary>
    void Delete(string storagePath);
}
