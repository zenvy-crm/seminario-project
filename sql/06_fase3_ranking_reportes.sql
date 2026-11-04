USE [SeminarioDb];
GO

-- =============================================================================
-- FASE 3 - Ranking de proveedores calculado por SQL Server (funcion RANK)
--   dbo.FN_Ranking_Proveedores(@Anio) -> funcion en linea (inline TVF), un solo SELECT.
--   Suma el Importe de las lineas VIGENTES (EstadoLinea = 1) de las ordenes NO anuladas del anio
--   y numera con RANK() OVER (ORDER BY SUM(Importe) DESC): los empates comparten posicion.
--   El filtro por FechaEmision (>= 1 de enero, < 1 de enero del anio siguiente) sobre la columna
--   de particion permite la eliminacion de particiones, y el join al detalle usa
--   (FechaEmision, OrdenId), la misma llave alineada con el encabezado.
-- En la aplicacion la crea la migracion EF 'Fase3ReportesRanking' (CREATE OR ALTER, idempotente).
-- El backend NUNCA ordena ni rankea en memoria: solo ejecuta
--   SELECT TOP (@top) * FROM dbo.FN_Ranking_Proveedores(@anio) ORDER BY PosicionRanking, ProveedorNit
-- =============================================================================
CREATE OR ALTER FUNCTION [dbo].[FN_Ranking_Proveedores] (@Anio smallint)
RETURNS TABLE
AS
RETURN
(
    SELECT
        CAST(RANK() OVER (ORDER BY SUM(d.[Importe]) DESC) AS int)   AS [PosicionRanking],
        o.[ProveedorNit]                                            AS [ProveedorNit],
        MAX(o.[ProveedorNombre])                                    AS [ProveedorNombre],
        CAST(SUM(d.[Importe]) AS decimal(18,2))                     AS [TotalComprometido],
        COUNT(DISTINCT o.[Id])                                      AS [Ordenes],
        COUNT(*)                                                    AS [Lineas],
        CAST(100.0 * SUM(d.[Importe]) / NULLIF(SUM(SUM(d.[Importe])) OVER (), 0) AS decimal(5,2)) AS [ParticipacionPct]
    FROM [dbo].[TB_Ordenes_Compra] o
    JOIN [dbo].[TB_Ordenes_Compra_Detalle] d
        ON d.[FechaEmision] = o.[FechaEmision] AND d.[OrdenId] = o.[Id]
    WHERE o.[FechaEmision] >= DATEFROMPARTS(@Anio, 1, 1)
      AND o.[FechaEmision] <  DATEFROMPARTS(@Anio + 1, 1, 1)
      AND o.[Estado] <> N'ANULADA'
      AND d.[EstadoLinea] = 1
    GROUP BY o.[ProveedorNit]
);
GO

-- Prueba rapida (comentada): los 5 mayores proveedores de 2026
-- SELECT TOP (5) * FROM dbo.FN_Ranking_Proveedores(2026) ORDER BY PosicionRanking, ProveedorNit;
GO
