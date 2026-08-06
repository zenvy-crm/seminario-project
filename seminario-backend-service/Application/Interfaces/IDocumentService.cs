using Microsoft.AspNetCore.Http;
using Seminario.Application.DTOs;

namespace Seminario.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(int userId, IFormFile file);
    Task<List<DocumentDto>> GetByUserIdAsync(int userId);
    Task DeleteAsync(int userId, int documentId);
}
