USE [SeminarioDb];
GO

-- =============================================================================
-- FASE 3 - Evidencia de particionamiento (para la demo en video)
-- Cuantas filas cayeron fisicamente en cada particion, para el encabezado y el detalle.
-- Ambas tablas usan PS_RangoFechasOrdenes (RANGE RIGHT: 2024, 2025, 2026, 2027).
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
