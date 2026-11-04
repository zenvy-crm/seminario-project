namespace Seminario.Domain.Entities;

public class UserDocument
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft Delete: el documento se marca como eliminado y deja de verse (filtro global de EF),
    /// pero la fila y el archivo en disco se conservan como evidencia.
    /// </summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>Usuario que ejecuto la eliminacion (claim sub del JWT): puede ser el dueno o un ADMIN.</summary>
    public int? DeletedByUserId { get; set; }

    /// <summary>
    /// Token de concurrencia optimista. SQL Server lo incrementa solo en cada UPDATE;
    /// EF Core lo agrega al WHERE y detecta actualizaciones perdidas sin bloquear filas.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
