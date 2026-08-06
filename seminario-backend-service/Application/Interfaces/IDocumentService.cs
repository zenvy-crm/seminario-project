using Microsoft.AspNetCore.Http;
using Seminario.Application.DTOs;

namespace Seminario.Application.Interfaces;

public record DownloadedFile(byte[] Content, string ContentType, string FileName);

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(int userId, IFormFile file);
    Task<List<DocumentDto>> GetByUserIdAsync(int userId);
    Task<DownloadedFile> DownloadAsync(int userId, int documentId);
    Task DeleteAsync(int userId, int documentId);
}
