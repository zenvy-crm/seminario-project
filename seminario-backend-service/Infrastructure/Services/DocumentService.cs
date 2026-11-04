using System.Data;
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

    /// <summary>
    /// Sube un documento dentro de una transaccion explicita.
    ///
    /// La operacion mezcla una escritura en disco con una escritura en la base, asi que
    /// SaveChangesAsync por si solo no alcanza: si la fila falla, el archivo quedaria
    /// huerfano. El catch compensa borrandolo; el finally encola la auditoria, que al
    /// ser una escritura en memoria no puede tumbar la transaccion.
    /// </summary>
    public async Task<DocumentDto> UploadAsync(int userId, IFormFile file)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new NotFoundException("El usuario no existe");
        }

        // Cuando se usa transaccion explicita, EF exige que el bloque completo viva dentro
        // de ExecuteAsync para que un reintento rehaga la transaccion entera.
        // Hoy Program.cs registra UseSqlServer sin EnableRetryOnFailure, asi que esta es la
        // estrategia no reintentante y el bloque se ejecuta una sola vez. El envoltorio queda
        // para poder activar los reintentos sin reescribir el metodo.
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            StoredFile? stored = null;
            UserDocument? document = null;
            AuditEvent? auditEvent = null;

            try
            {
                try
                {
                    stored = await _fileStorage.ValidateAndStoreAsync(file, userId);
                }
                catch (ApiException)
                {
                    // Archivo rechazado por las validaciones: se audita el intento y se
                    // propaga. No hay nada que compensar porque no se escribio nada.
                    auditEvent = new AuditEvent
                    {
                        EventType = AuditEventTypes.DocumentRejected,
                        Entity = "UserDocument",
                        UserId = userId,
                        After = new { file.FileName, file.ContentType, file.Length }
                    };
                    throw;
                }

                document = new UserDocument
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

                await tx.CommitAsync();   // punto de durabilidad: a partir de aqui la fila es ACID

                auditEvent = new AuditEvent
                {
                    EventType = AuditEventTypes.DocumentUploaded,
                    Entity = "UserDocument",
                    EntityId = document.Id.ToString(),
                    UserId = userId,
                    After = new { document.FileName, document.ContentType, document.FileSize }
                };

                return ToDto(document);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Otro proceso modifico o elimino la fila primero (rowversion no coincide).
                await RollbackYCompensarAsync(tx, stored);
                throw new ConflictException("El documento fue modificado por otro usuario. Vuelva a intentarlo.", ex);
            }
            catch (Exception)
            {
                await RollbackYCompensarAsync(tx, stored);
                throw;   // el middleware global lo traduce a la respuesta HTTP
            }
            finally
            {
                // Encolar SIEMPRE: es una escritura en memoria, sin E/S, de modo que ni
                // retrasa la respuesta ni puede provocar el fallo de la transaccion.
                if (auditEvent is not null)
                {
                    _auditLogger.Enqueue(auditEvent);
                }
            }
        });
    }

    /// <summary>Deshace la transaccion y borra el archivo ya escrito en disco.</summary>
    private async Task RollbackYCompensarAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx,
        StoredFile? stored)
    {
        await tx.RollbackAsync();

        if (stored is not null)
        {
            _fileStorage.Delete(stored.StoragePath);
        }

        // El DbContext queda con la entidad en estado Added tras el rollback: hay que
        // limpiarlo para que un reintento de la estrategia no la reenvie duplicada.
        _db.ChangeTracker.Clear();
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

    /// <summary>
    /// Soft Delete: marca la fila como eliminada (UPDATE, nunca DELETE). El filtro global de EF
    /// la oculta del listado, de la descarga y del perfil del usuario. El archivo en disco se
    /// conserva: es la evidencia de lo que el proveedor entrego.
    /// </summary>
    public async Task DeleteAsync(int userId, int documentId, int actorUserId)
    {
        var document = await _db.UserDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId)
            ?? throw new NotFoundException("El documento no existe");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            AuditEvent? auditEvent = null;

            try
            {
                document.IsDeleted = true;
                document.DeletedAt = DateTime.UtcNow;
                document.DeletedByUserId = actorUserId;

                // RowVersion va en el WHERE del UPDATE: si otra peticion ya lo elimino, salta la
                // DbUpdateConcurrencyException de abajo en lugar de pisar el cambio.
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                auditEvent = new AuditEvent
                {
                    EventType = AuditEventTypes.DocumentDeleted,
                    Entity = "UserDocument",
                    EntityId = documentId.ToString(),
                    UserId = userId,
                    Before = new { document.FileName, document.ContentType, IsDeleted = false },
                    After = new { IsDeleted = true, document.DeletedAt, document.DeletedByUserId }
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                throw new ConflictException("El documento ya fue eliminado o modificado por otro usuario.", ex);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                throw;
            }
            finally
            {
                if (auditEvent is not null)
                {
                    _auditLogger.Enqueue(auditEvent);
                }
            }
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
