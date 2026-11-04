using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seminario.Api;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.Interfaces;

namespace Seminario.Controllers;

[ApiController]
[Route("api/users/{userId:int}/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentDto>>> GetDocuments(int userId)
    {
        EnsureSelfOrAdmin(userId);
        return Ok(await _documentService.GetByUserIdAsync(userId));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentDto>> Upload(int userId, IFormFile file)
    {
        EnsureSelfOrAdmin(userId);
        return Ok(await _documentService.UploadAsync(userId, file));
    }

    [HttpGet("{documentId:int}/content")]
    public async Task<IActionResult> Download(int userId, int documentId)
    {
        EnsureSelfOrAdmin(userId);
        var file = await _documentService.DownloadAsync(userId, documentId);
        return File(file.Content, file.ContentType);
    }

    /// <summary>
    /// Soft Delete: marca el documento como eliminado; la fila y el archivo se conservan.
    /// </summary>
    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int userId, int documentId)
    {
        EnsureSelfOrAdmin(userId);
        await _documentService.DeleteAsync(userId, documentId, User.GetUserId());
        return NoContent();
    }

    private void EnsureSelfOrAdmin(int userId)
    {
        if (!User.IsAdmin() && User.GetUserId() != userId)
        {
            throw new ForbiddenException("No tiene permisos para acceder a los documentos de otro usuario");
        }
    }
}
