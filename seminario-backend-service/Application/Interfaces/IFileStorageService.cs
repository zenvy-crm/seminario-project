using Microsoft.AspNetCore.Http;

namespace Seminario.Application.Interfaces;

public record StoredFile(string OriginalFileName, string StoredFileName, string ContentType, long Size, string StoragePath);

public interface IFileStorageService
{
    Task ValidateOnlyAsync(IFormFile file);
    Task<StoredFile> ValidateAndStoreAsync(IFormFile file, int userId);
    Task<byte[]> ReadAsync(string storagePath);
}
