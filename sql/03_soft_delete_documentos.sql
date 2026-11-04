USE [SeminarioDb];
GO

-- =============================================================================
-- Soft Delete de documentos (UserDocuments)
-- En la aplicacion lo crea la migracion EF 'UserDocumentSoftDelete'. Este script es el
-- equivalente para ejecutar a mano; es idempotente.
-- El DELETE /api/users/{userId}/documents/{documentId} ya no borra la fila ni el archivo:
-- marca IsDeleted = 1 y el filtro global de EF oculta el documento de toda consulta.
-- =============================================================================


-- =============================================================================
-- PASO 1: Columnas de Soft Delete
-- =============================================================================
IF COL_LENGTH('dbo.UserDocuments', 'IsDeleted') IS NULL
    ALTER TABLE [dbo].[UserDocuments] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_UserDocuments_IsDeleted] DEFAULT (0);
GO
IF COL_LENGTH('dbo.UserDocuments', 'DeletedAt') IS NULL
    ALTER TABLE [dbo].[UserDocuments] ADD [DeletedAt] datetime2(0) NULL;
GO
IF COL_LENGTH('dbo.UserDocuments', 'DeletedByUserId') IS NULL
    ALTER TABLE [dbo].[UserDocuments] ADD [DeletedByUserId] int NULL;
GO


-- =============================================================================
-- PASO 2: La FK hacia Users deja de ser ON DELETE CASCADE
-- SQL Server no permite un trigger INSTEAD OF DELETE en una tabla con FK en cascada.
-- =============================================================================
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_UserDocuments_Users_UserId' AND delete_referential_action_desc = 'CASCADE')
BEGIN
    ALTER TABLE [dbo].[UserDocuments] DROP CONSTRAINT [FK_UserDocuments_Users_UserId];
    ALTER TABLE [dbo].[UserDocuments] ADD CONSTRAINT [FK_UserDocuments_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]);
END;
GO


-- =============================================================================
-- PASO 3: DELETE fisico prohibido (mismo codigo 50020 que las tablas de ordenes)
-- =============================================================================
CREATE OR ALTER TRIGGER [dbo].[TR_UserDocuments_NoDelete]
ON [dbo].[UserDocuments]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    THROW 50020, N'Borrado fisico prohibido: use Soft Delete', 1;
END;
GO


-- =============================================================================
-- PASO 4: PRUEBAS
-- =============================================================================
/*
-- Documentos eliminados logicamente (siguen en la tabla; la API ya no los muestra)
SELECT Id, UserId, FileName, IsDeleted, DeletedAt, DeletedByUserId
FROM dbo.UserDocuments
WHERE IsDeleted = 1
ORDER BY DeletedAt DESC;

-- Borrado fisico: debe fallar con el error 50020
DELETE FROM dbo.UserDocuments WHERE Id = -1;          -- sin filas: no hace nada
DELETE TOP (1) FROM dbo.UserDocuments;                 -- Msg 50020: Borrado fisico prohibido
*/
