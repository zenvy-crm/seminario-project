USE [SeminarioDb];
GO

-- =============================================================================
-- PASO 1: Funcion de particion
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'FN_RangoFechasOrdenes')
BEGIN
    CREATE PARTITION FUNCTION FN_RangoFechasOrdenes (datetime2(0))
    AS RANGE RIGHT FOR VALUES
    (
        '2024-01-01',  
        '2025-01-01',  
        '2026-01-01',   
        '2027-01-01'   
    );
END;

GO

-- =============================================================================
-- PASO 2: Esquema de particion
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'PS_RangoFechasOrdenes')
BEGIN
    CREATE PARTITION SCHEME PS_RangoFechasOrdenes
    AS PARTITION FN_RangoFechasOrdenes
    ALL TO ([PRIMARY]);
END;
GO


IF EXISTS (
    SELECT 1
    FROM sys.partition_schemes
    WHERE name = 'PS_RangoFechasOrdenes'
)
BEGIN
    DROP PARTITION SCHEME PS_RangoFechasOrdenes;
END;

GO
-- =============================================================================
-- PASO 3: Tabla particionada fuertemente tipada
-- =============================================================================
IF OBJECT_ID('TB_Ordenes_Compra') IS NULL
BEGIN
    CREATE TABLE [TB_Ordenes_Compra] (
        [Id]               uniqueidentifier NOT NULL DEFAULT NEWID(),
        [FechaEmision]     datetime2(0)     NOT NULL,   -- columna de particion
        [NumeroOrden]      nvarchar(30)     NOT NULL,
        [UserId]           int              NOT NULL,   -- proviene del JWT, no del cliente
        [ProveedorNit]     nvarchar(20)     NOT NULL,
        [ProveedorNombre]  nvarchar(150)    NOT NULL,
        [Monto]            decimal(18,2)    NOT NULL,
        [Moneda]           nvarchar(3)      NOT NULL,
        [Estado]           nvarchar(20)     NOT NULL,
        [BodegaDestino]    nvarchar(60)     NOT NULL,
        [Detalle]          nvarchar(255)    NULL,
        [CreatedAt]        datetime2(0)     NOT NULL,
        CONSTRAINT [PK_TB_Ordenes_Compra] PRIMARY KEY CLUSTERED ([FechaEmision], [Id])
    ) ON PS_RangoFechasOrdenes([FechaEmision]);

    -- Indices alineados: al crearse sobre el mismo esquema quedan particionados
    -- igual que la tabla y conservan la eliminacion de particiones.
    CREATE NONCLUSTERED INDEX [IX_Ordenes_Fecha_Usuario]
        ON [TB_Ordenes_Compra] ([FechaEmision], [UserId])
        ON PS_RangoFechasOrdenes([FechaEmision]);

    CREATE NONCLUSTERED INDEX [IX_Ordenes_Fecha_Proveedor]
        ON [TB_Ordenes_Compra] ([FechaEmision], [ProveedorNit])
        ON PS_RangoFechasOrdenes([FechaEmision]);
END;

GO

-- =============================================================================
-- PASO 4: Data seed de respaldo
-- =============================================================================
SET NOCOUNT ON;
GO

DECLARE @Contador INT = 1;
DECLARE @FechaRandom DATETIME2(0);

WHILE @Contador <= 10000
BEGIN
    -- Fechas aleatorias entre 2023-01-01 y 2026-12-31
    SET @FechaRandom = DATEADD(DAY, ABS(CHECKSUM(NEWID()) % 1461), '2023-01-01');

    INSERT INTO TB_Ordenes_Compra
        (Id, FechaEmision, NumeroOrden, UserId, ProveedorNit, ProveedorNombre,
         Monto, Moneda, Estado, BodegaDestino, Detalle, CreatedAt)
    VALUES (
        NEWID(),
        @FechaRandom,
        CONCAT('OC-', FORMAT(@FechaRandom, 'yyyyMM'), '-', RIGHT(CONVERT(varchar(36), NEWID()), 8)),
        1,
        CONCAT(ABS(CHECKSUM(NEWID()) % 89999999 + 10000000), '-', ABS(CHECKSUM(NEWID()) % 10)),
        'Proveedor Generado Automaticamente',
        CAST(ABS(CHECKSUM(NEWID()) % 4990000) / 100.0 + 100 AS decimal(18,2)),
        CASE WHEN ABS(CHECKSUM(NEWID()) % 10) = 0 THEN 'USD' ELSE 'GTQ' END,
        CASE ABS(CHECKSUM(NEWID()) % 4)
            WHEN 0 THEN 'EMITIDA' WHEN 1 THEN 'RECIBIDA'
            WHEN 2 THEN 'PAGADA'  ELSE 'ANULADA' END,
        'Bodega Central Zona 12',
        'Tx generada automaticamente',
        SYSUTCDATETIME()
    );

    SET @Contador = @Contador + 1;
END;
GO

-- =============================================================================
-- PASO 5: LA AUDITORIA DEL ARQUITECTO
-- Demuestra que SQL Server corto fisicamente la tabla y cuantas filas cayeron
-- =============================================================================
SELECT
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
WHERE p.object_id = OBJECT_ID('TB_Ordenes_Compra') AND i.index_id <= 1
ORDER BY p.partition_number;
GO

-- =============================================================================
-- PASO 5: LA AUDITORIA DEL ARQUITECTO
-- =============================================================================

SELECT 
    p.partition_number AS Numero_Particion,
    f.name AS Archivo_Fisico,
    p.rows AS Registros_En_Este_Fragmento
FROM sys.partitions p
JOIN sys.destination_data_spaces dds ON p.partition_number = dds.destination_id
JOIN sys.filegroups f ON dds.data_space_id = f.data_space_id
JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
WHERE p.object_id = OBJECT_ID('TB_Ordenes_Compra') AND i.type <= 1;



select COUNT(1) from TB_Ordenes_Compra;