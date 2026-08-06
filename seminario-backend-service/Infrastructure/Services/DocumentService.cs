using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Seminario.Application.Audit;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.Interfaces;
using Seminario.Domain.Entities;
using Seminario.Infrastructure.Persistence;

namespace Seminario.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditLogger _auditLogger;

    public DocumentService(AppDbContext db, IFileStorageService fileStorage, IAuditLogger auditLogger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _auditLogger = auditLogger;
    }

    public async Task<DocumentDto> UploadAsync(int userId, IFormFile file)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new NotFoundException("El usuario no existe");
        }

        StoredFile stored;
        try
        {
            stored = await _fileStorage.ValidateAndStoreAsync(file, userId);
        }
        catch (ApiException)
        {
            _auditLogger.Enqueue(new AuditEvent
            {
                EventType = AuditEventTypes.DocumentRejected,
                Entity = "UserDocument",
                UserId = userId,
                After = new { file.FileName, file.ContentType, file.Length }
            });
            throw;
        }

        var document = new UserDocument
        {
            UserId = userId,
            FileName = stored.OriginalFileName,
            StoredFileName = stored.StoredFileName,
            ContentType = stored.ContentType,
            FileSize = stored.Size,
            StoragePath = stored.StoragePath,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserDocuments.Add(document);
        await _db.SaveChangesAsync();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.DocumentUploaded,
            Entity = "UserDocument",
            EntityId = document.Id.ToString(),
            UserId = userId,
            After = new { document.FileName, document.ContentType, document.FileSize }
        });

        return ToDto(document);
    }

    public async Task<List<DocumentDto>> GetByUserIdAsync(int userId)
    {
        var documents = await _db.UserDocuments
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(ToDto).ToList();
    }

    public async Task<DownloadedFile> DownloadAsync(int userId, int documentId)
    {
        var document = await _db.UserDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId)
            ?? throw new NotFoundException("El documento no existe");

        var content = await _fileStorage.ReadAsync(document.StoragePath);
        return new DownloadedFile(content, document.ContentType, document.FileName);
    }

    public async Task DeleteAsync(int userId, int documentId)
    {
        var document = await _db.UserDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId)
            ?? throw new NotFoundException("El documento no existe");

        _db.UserDocuments.Remove(document);
        await _db.SaveChangesAsync();

        _auditLogger.Enqueue(new AuditEvent
        {
            EventType = AuditEventTypes.DocumentDeleted,
            Entity = "UserDocument",
            EntityId = documentId.ToString(),
            UserId = userId,
            Before = new { document.FileName, document.ContentType }
        });
    }

    private static DocumentDto ToDto(UserDocument document) => new()
    {
        Id = document.Id,
        FileName = document.FileName,
        MimeType = document.ContentType,
        SizeBytes = document.FileSize,
        UploadedAt = document.CreatedAt,
        Url = $"/api/users/{document.UserId}/documents/{document.Id}/content"
    };
}
