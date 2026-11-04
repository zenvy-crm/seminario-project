USE [SeminarioDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Lote        int;
DECLARE @Prefijo     nvarchar(20);
DECLARE @Semilla     int = ABS(CHECKSUM(NEWID())); 
DECLARE @Encabezados TABLE (FechaEmision datetime2(0) NOT NULL, Id uniqueidentifier NOT NULL, PRIMARY KEY (FechaEmision, Id));
DECLARE @t0          datetime2(7);
DECLARE @MsEnc       bigint;
DECLARE @MsDet       bigint;

SELECT @Lote = 1 + ISNULL(MAX(TRY_CAST(SUBSTRING(NumeroOrden, 7, CHARINDEX('-', NumeroOrden, 7) - 7) AS int)), 0)
FROM dbo.TB_Ordenes_Compra
WHERE NumeroOrden LIKE N'BD26-L[0-9]%-[0-9]%';

SET @Prefijo = CONCAT(N'BD26-L', FORMAT(@Lote, '00'), N'-');
PRINT CONCAT('Lote ', @Lote, ' (prefijo ', @Prefijo, ')');

BEGIN TRY
    BEGIN TRAN;

    -- ---------------------------------------------------------------------------------
    -- INSERT 1: 250 encabezados en el año  2026 (2026-01-01 hasta el  2026-09-28)
    -- ---------------------------------------------------------------------------------
    SET @t0 = SYSDATETIME();

    WITH CTE_Numeros AS
    (
        SELECT TOP (250) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
        FROM master.dbo.spt_values t1
    )
    INSERT INTO dbo.TB_Ordenes_Compra
        (Id, FechaEmision, NumeroOrden, UserId, ProveedorNit, ProveedorNombre,
         Monto, Moneda, Estado, BodegaDestino, Detalle, CreatedAt)
    OUTPUT inserted.FechaEmision, inserted.Id INTO @Encabezados (FechaEmision, Id)
    SELECT
        NEWID(),
        DATEADD(DAY, ABS(CHECKSUM(NEWID())) % 271, CAST('2026-01-01' AS datetime2(0))),  -- 271 dias
        CONCAT(@Prefijo, FORMAT(n.N, '000000')),
        1,
        pv.Nit,
        pv.Nombre,
        0,                                   -- lo recalcula el trigger con la suma de lineas
        N'GTQ',
        N'EMITIDA',
        bg.Bodega,
        CONCAT(N'Carga masiva Fase 3 (lote ', @Lote, N')'),
        SYSUTCDATETIME()
    FROM CTE_Numeros n
    JOIN (VALUES
            (0,  N'2456781-3', N'Distribuidora La Fragua, S.A.'),
            (1,  N'1087452-6', N'Cementos Progreso, S.A.'),
            (2,  N'3345120-9', N'Alimentos Kerns y Cia.'),
            (3,  N'5561238-1', N'Ferreteria El Constructor, S.A.'),
            (4,  N'7734509-K', N'Aceros de Guatemala, S.A.'),
            (5,  N'2298164-5', N'Comercial Los Andes, S.A.'),
            (6,  N'4410927-2', N'Plasticos Industriales de Centroamerica'),
            (7,  N'6672033-8', N'Agroindustria Las Verapaces'),
            (8,  N'8891746-4', N'Papeles y Empaques del Norte'),
            (9,  N'9903215-7', N'Lubricantes y Repuestos Zona 4'),
            (10, N'1156378-0', N'Textiles San Cristobal, S.A.'),
            (11, N'3027745-3', N'Quimicos Industriales Guatemala')
         ) AS pv(Idx, Nit, Nombre)
        ON pv.Idx = CAST(12.0 * POWER((CAST(SUBSTRING(HASHBYTES('SHA1', CONCAT(@Semilla, N'-', n.N)), 1, 4) AS bigint) % 10000) / 10000.0, 2) AS int)
    JOIN (VALUES
            (0, N'Bodega Norte Chimaltenango'),
            (1, N'Bodega Regional Quetzaltenango'),
            (2, N'Centro de Distribucion Villa Nueva'),
            (3, N'Cross-dock Puerto Barrios')
         ) AS bg(Idx, Bodega)
        ON bg.Idx = n.N % 4;

    SET @MsEnc = DATEDIFF_BIG(ms, @t0, SYSDATETIME());
    PRINT CONCAT('INSERT 1 (encabezados): ', @MsEnc, ' ms');

    -- ---------------------------------------------------------------------------------
    -- INSERT 2: 200 lineas por cada encabezado de ESTE lote = 250 x 200 = 50,000 filas
    -- ---------------------------------------------------------------------------------
    SET @t0 = SYSDATETIME();

    WITH CTE_Lineas AS
    (
        SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Linea
        FROM master.dbo.spt_values t2
    )
    INSERT INTO dbo.TB_Ordenes_Compra_Detalle
        (FechaEmision, OrdenId, Linea, Descripcion, Cantidad, PrecioUnitario, EstadoLinea)
    SELECT
        e.FechaEmision,
        e.Id,
        l.Linea,
        pr.Descripcion,
        CAST(1 + ABS(CHECKSUM(NEWID())) % 5 AS decimal(18,3)),
        CAST(1 + (ABS(CHECKSUM(NEWID())) % 1900) / 100.0 AS decimal(18,4)),
        1
    FROM @Encabezados e
    CROSS JOIN CTE_Lineas l
    JOIN (VALUES
            (0, N'Cemento gris 42.5kg'),
            (1, N'Varilla corrugada 3/8'),
            (2, N'Harina de trigo 50 lb'),
            (3, N'Aceite lubricante 20W-50'),
            (4, N'Lamina troquelada calibre 26'),
            (5, N'Resma de papel bond carta'),
            (6, N'Tubo PVC 4 pulgadas'),
            (7, N'Azucar refinada 25 kg'),
            (8, N'Guantes de carnaza industriales'),
            (9, N'Pintura latex blanca (cubeta)')
         ) AS pr(Idx, Descripcion)
        ON pr.Idx = ABS(CHECKSUM(e.Id, l.Linea)) % 10;

    SET @MsDet = DATEDIFF_BIG(ms, @t0, SYSDATETIME());
    PRINT CONCAT('INSERT 2 (detalle): ', @MsDet, ' ms');

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRAN;
    THROW;
END CATCH;


-- Resumen de la carga (el trigger ya recalculo Monto de cada encabezado)
SELECT
    @Lote                                               AS Lote,
    (SELECT COUNT(*) FROM @Encabezados)                 AS Encabezados,
    (SELECT COUNT(*) FROM dbo.TB_Ordenes_Compra_Detalle d
       JOIN @Encabezados e ON e.FechaEmision = d.FechaEmision AND e.Id = d.OrdenId)   AS Lineas,
    (SELECT SUM(o.Monto) FROM dbo.TB_Ordenes_Compra o
       JOIN @Encabezados e ON e.FechaEmision = o.FechaEmision AND e.Id = o.Id)        AS Total_GTQ,
    @MsEnc                                              AS Ms_Insert_Encabezados,
    @MsDet                                              AS Ms_Insert_Detalle,
    @MsEnc + @MsDet                                     AS Ms_Total;

-- Monto comprometido por bodega despues del lote (debe quedar cerca de 0.4M por bodega)
SELECT o.BodegaDestino, COUNT(*) AS Ordenes, SUM(o.Monto) AS Monto_Lote
FROM dbo.TB_Ordenes_Compra o
JOIN @Encabezados e ON e.FechaEmision = o.FechaEmision AND e.Id = o.Id
GROUP BY o.BodegaDestino
ORDER BY o.BodegaDestino;

-- -------------------------------------------------------------------------------------
-- Datos de soft delete para el reporte de auditoria
-- Se filtra por FechaEmision para que SQL Server elimine particiones.
-- -------------------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRAN;

    WITH Anular AS
    (
        SELECT TOP (10) o.*
        FROM dbo.TB_Ordenes_Compra o
        WHERE o.FechaEmision >= '2026-01-01' AND o.FechaEmision < '2027-01-01'
          AND o.NumeroOrden LIKE @Prefijo + N'%'
        ORDER BY o.NumeroOrden
    )
    UPDATE Anular
    SET Estado = N'ANULADA',
        UsuarioAnulaId = 1,
        FechaAnulacion = SYSUTCDATETIME(),
        MotivoAnulacion = CONCAT(N'Anulacion de prueba Fase 3 (lote ', @Lote, N')');

    DECLARE @Anuladas int = @@ROWCOUNT;

    UPDATE d
    SET d.EstadoLinea = 0,
        d.UsuarioBajaId = 1,
        d.FechaBaja = SYSUTCDATETIME(),
        d.MotivoBaja = CONCAT(N'Baja de linea de prueba Fase 3 (lote ', @Lote, N')')
    FROM dbo.TB_Ordenes_Compra_Detalle d
    JOIN (
        SELECT o.FechaEmision, o.Id
        FROM dbo.TB_Ordenes_Compra o
        WHERE o.FechaEmision >= '2026-01-01' AND o.FechaEmision < '2027-01-01'
          AND o.NumeroOrden LIKE @Prefijo + N'%'
          AND o.Estado <> N'ANULADA'
        ORDER BY o.NumeroOrden
        OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
    ) x ON x.FechaEmision = d.FechaEmision AND x.Id = d.OrdenId
    WHERE d.Linea <= 5 AND d.EstadoLinea = 1;

    DECLARE @LineasBaja int = @@ROWCOUNT;

    COMMIT TRAN;
    PRINT CONCAT('Soft delete: ', @Anuladas, ' encabezados ANULADA, ', @LineasBaja, ' lineas con EstadoLinea = 0');
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRAN;
    THROW;
END CATCH;
GO

-- =============================================================================
-- EVIDENCIA: filas por particion en el encabezado y en el detalle
-- =============================================================================
SELECT
    OBJECT_NAME(p.object_id)                                 AS Tabla,
    p.partition_number                                       AS Numero_Particion,
    fg.name                                                  AS Filegroup_Fisico,
    ISNULL(CONVERT(varchar(10), rv_izq.value, 23), '-inf')   AS Rango_Desde,
    ISNULL(CONVERT(varchar(10), rv_der.value, 23), '+inf')   AS Rango_Hasta,
    p.rows                                                   AS Registros_En_Este_Fragmento
FROM sys.partitions p
JOIN sys.indexes i
    ON p.object_id = i.object_id AND p.index_id = i.index_id
JOIN sys.destination_data_spaces dds
    ON dds.partition_scheme_id = i.data_space_id AND dds.destination_id = p.partition_number
JOIN sys.filegroups fg
    ON dds.data_space_id = fg.data_space_id
JOIN sys.partition_schemes ps
    ON ps.data_space_id = i.data_space_id
JOIN sys.partition_functions pf
    ON pf.function_id = ps.function_id
LEFT JOIN sys.partition_range_values rv_izq
    ON rv_izq.function_id = pf.function_id AND rv_izq.boundary_id = p.partition_number - 1
LEFT JOIN sys.partition_range_values rv_der
    ON rv_der.function_id = pf.function_id AND rv_der.boundary_id = p.partition_number
WHERE p.object_id IN (OBJECT_ID('dbo.TB_Ordenes_Compra'), OBJECT_ID('dbo.TB_Ordenes_Compra_Detalle'))
  AND i.index_id IN (0, 1)
ORDER BY Tabla, p.partition_number;
GO
