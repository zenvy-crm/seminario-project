using Microsoft.AspNetCore.Http;
using Seminario.Application.DTOs;

namespace Seminario.Application.Interfaces;

public record DownloadedFile(byte[] Content, string ContentType, string FileName);

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(int userId, IFormFile file);
    Task<List<DocumentDto>> GetByUserIdAsync(int userId);
    Task<DownloadedFile> DownloadAsync(int userId, int documentId);
    /// <summary>Soft Delete del documento.</summary>
    /// <param name="actorUserId">Quien ejecuta la eliminacion, tomado del JWT por el controlador.</param>
    Task DeleteAsync(int userId, int documentId, int actorUserId);
}
