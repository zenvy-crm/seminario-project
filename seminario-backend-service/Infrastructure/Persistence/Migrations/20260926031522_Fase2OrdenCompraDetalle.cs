using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seminario_backend_service.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Fase 2: detalle de la orden de compra (particionado igual que el encabezado), presupuesto de
    /// compras por bodega (la "capacidad"), SP transaccional que recibe JSON, triggers de presupuesto
    /// y de prohibicion de borrado fisico, columnas de soft delete en el encabezado y rol INTERMEDIO.
    /// Todo va en SQL nativo: EF no sabe expresar particiones, SP ni triggers. Cada CREATE PROCEDURE /
    /// CREATE TRIGGER va en su propio migrationBuilder.Sql() porque debe ser el unico statement del batch.
    /// </summary>
    public partial class Fase2OrdenCompraDetalle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cada paso DDL lleva su guarda (IF ... IS NULL / NOT EXISTS), igual que sql/02: si alguien
            // ya corrio ese script sobre la base, la migracion no choca con los objetos que encuentre.

            // PASO 1: Columnas de soft delete y concurrencia en el encabezado. Todas son NULL o se
            // llenan solas (rowversion), asi el stress test con SqlBulkCopy de 12 columnas sigue igual.
            // Nota: agregar rowversion recorre toda la tabla; DbSeeder y AppDbContextFactory suben el
            // CommandTimeout para que no corte en 30 s si el stress test la lleno.
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.TB_Ordenes_Compra', 'RowVer') IS NULL
BEGIN
    ALTER TABLE [dbo].[TB_Ordenes_Compra] ADD
        [UsuarioAnulaId]   int              NULL,
        [FechaAnulacion]   datetime2(0)     NULL,
        [MotivoAnulacion]  nvarchar(200)    NULL,
        [RowVer]           rowversion       NOT NULL;
END;");

            // Indice alineado por Id: permite ubicar una orden sin conocer su FechaEmision
            // (un seek por particion en lugar de un scan de toda la tabla).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ordenes_Id' AND object_id = OBJECT_ID('dbo.TB_Ordenes_Compra'))
    CREATE NONCLUSTERED INDEX [IX_Ordenes_Id]
        ON [dbo].[TB_Ordenes_Compra] ([Id])
        ON PS_RangoFechasOrdenes([FechaEmision]);");

            // PASO 2: Presupuesto de compras por bodega y anio. Es la capacidad que descuenta el trigger.
            // CK_Presupuesto_Bodega_Comprometido: el comprometido nunca puede quedar negativo.
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.TB_Presupuesto_Bodega') IS NULL
BEGIN
    CREATE TABLE [dbo].[TB_Presupuesto_Bodega] (
        [Anio]               smallint       NOT NULL,
        [Bodega]             nvarchar(60)   NOT NULL,
        [MontoAsignado]      decimal(18,2)  NOT NULL,
        [MontoComprometido]  decimal(18,2)  NOT NULL CONSTRAINT [DF_Presupuesto_Bodega_Comprometido] DEFAULT (0),
        [MontoDisponible]    AS CAST([MontoAsignado] - [MontoComprometido] AS decimal(18,2)) PERSISTED,
        [RowVer]             rowversion     NOT NULL,
        CONSTRAINT [PK_TB_Presupuesto_Bodega] PRIMARY KEY CLUSTERED ([Anio], [Bodega]),
        CONSTRAINT [CK_Presupuesto_Bodega_Asignado] CHECK ([MontoAsignado] >= 0)
    );
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Presupuesto_Bodega_Comprometido')
    ALTER TABLE [dbo].[TB_Presupuesto_Bodega]
        ADD CONSTRAINT [CK_Presupuesto_Bodega_Comprometido] CHECK ([MontoComprometido] >= 0);");

            // PASO 3: Detalle particionado con el MISMO esquema que el encabezado: cada linea cae en la
            // misma particion que su orden y la FK compuesta (FechaEmision, OrdenId) queda alineada.
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.TB_Ordenes_Compra_Detalle') IS NULL
BEGIN
    CREATE TABLE [dbo].[TB_Ordenes_Compra_Detalle] (
        [FechaEmision]    datetime2(0)      NOT NULL,
        [OrdenId]         uniqueidentifier  NOT NULL,
        [Linea]           int               NOT NULL,
        [Descripcion]     nvarchar(150)     NOT NULL,
        [Cantidad]        decimal(18,3)     NOT NULL,
        [PrecioUnitario]  decimal(18,4)     NOT NULL,
        [Importe]         AS CAST(ROUND([Cantidad] * [PrecioUnitario], 2) AS decimal(18,2)) PERSISTED,
        [EstadoLinea]     bit               NOT NULL CONSTRAINT [DF_OC_Detalle_EstadoLinea] DEFAULT (1),
        [UsuarioBajaId]   int               NULL,
        [FechaBaja]       datetime2(0)      NULL,
        [MotivoBaja]      nvarchar(200)     NULL,
        [RowVer]          rowversion        NOT NULL,
        CONSTRAINT [PK_TB_Ordenes_Compra_Detalle] PRIMARY KEY CLUSTERED ([FechaEmision], [OrdenId], [Linea]),
        CONSTRAINT [CK_OC_Detalle_Cantidad] CHECK ([Cantidad] > 0),
        CONSTRAINT [CK_OC_Detalle_Precio] CHECK ([PrecioUnitario] >= 0),
        CONSTRAINT [FK_OC_Detalle_Encabezado] FOREIGN KEY ([FechaEmision], [OrdenId])
            REFERENCES [dbo].[TB_Ordenes_Compra] ([FechaEmision], [Id])
    ) ON PS_RangoFechasOrdenes([FechaEmision]);
END;");

            // PASO 4: Secuencia para el correlativo de NumeroOrden (OC-yyyy-NNNNNN).
            migrationBuilder.Sql(SecuenciaNumeroOrden);

            // PASO 5: Semilla del presupuesto (3 bodegas x 4 anios, idempotente).
            migrationBuilder.Sql(@"
INSERT INTO [dbo].[TB_Presupuesto_Bodega] ([Anio], [Bodega], [MontoAsignado])
SELECT a.[Anio], b.[Bodega], 2500000.00
FROM (VALUES (2024), (2025), (2026), (2027)) AS a([Anio])
CROSS JOIN (VALUES (N'GT-CENTRO'), (N'GT-NORTE'), (N'GT-SUR')) AS b([Bodega])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[TB_Presupuesto_Bodega] p
    WHERE p.[Anio] = a.[Anio] AND p.[Bodega] = b.[Bodega]);");

            // PASO 6: Rol INTERMEDIO (nivel 2). DbSeeder tambien lo crea por nombre si falta.
            migrationBuilder.Sql(@"
INSERT INTO [dbo].[Roles] ([Name], [Description])
SELECT N'INTERMEDIO', N'Usuario intermedio: puede anular lineas del detalle de una orden'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'INTERMEDIO');");

            // PASO 7: Trigger de presupuesto sobre el detalle.
            migrationBuilder.Sql(TriggerDetallePresupuesto);

            // PASO 8: Trigger de anulacion del encabezado.
            migrationBuilder.Sql(TriggerEncabezadoAnulacion);

            // PASO 9: Borrado fisico prohibido en ambas tablas.
            migrationBuilder.Sql(TriggerEncabezadoNoDelete);
            migrationBuilder.Sql(TriggerDetalleNoDelete);

            // PASO 10: Stored Procedure transaccional que recibe la orden completa en JSON.
            migrationBuilder.Sql(SpOrdenCompraCrear);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SP_OrdenCompra_Crear];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_OC_Detalle_NoDelete];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_OC_Encabezado_NoDelete];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_OC_Encabezado_Anulacion];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_OC_Detalle_Presupuesto];");

            // El rol solo se quita si ningun usuario lo tiene asignado (FK_Users_Roles_RoleId).
            migrationBuilder.Sql(@"
DELETE r FROM [dbo].[Roles] r
WHERE r.[Name] = N'INTERMEDIO'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[Users] u WHERE u.[RoleId] = r.[Id]);");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS [dbo].[SEQ_OrdenCompra_Numero];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[TB_Ordenes_Compra_Detalle];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[TB_Presupuesto_Bodega];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Ordenes_Id] ON [dbo].[TB_Ordenes_Compra];");
            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[TB_Ordenes_Compra] DROP COLUMN
    [RowVer], [MotivoAnulacion], [FechaAnulacion], [UsuarioAnulaId];");
        }

        // La secuencia arranca despues del mayor correlativo OC-yyyy-NNNNNN ya grabado: si la migracion
        // se revierte (Down borra la secuencia pero no los encabezados) y se vuelve a aplicar, no se
        // repiten numeros. El formato del stress test (OC-yyyyMM-NNNNNNNN) no calza con el patron.
        private const string SecuenciaNumeroOrden = @"
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'SEQ_OrdenCompra_Numero')
BEGIN
    DECLARE @Inicio bigint = 1 + ISNULL((
        SELECT MAX(TRY_CAST(SUBSTRING([NumeroOrden], 9, 20) AS int))
        FROM [dbo].[TB_Ordenes_Compra]
        WHERE [NumeroOrden] LIKE N'OC-[0-9][0-9][0-9][0-9]-[0-9]%'), 0);
    DECLARE @Sql nvarchar(400) = CONCAT(
        N'CREATE SEQUENCE [dbo].[SEQ_OrdenCompra_Numero] AS int START WITH ', @Inicio, N' INCREMENT BY 1 NO CACHE;');
    EXEC sys.sp_executesql @Sql;
END;";

        // AFTER INSERT, UPDATE sobre el detalle, por conjuntos (soporta multi-fila):
        //  - Delta por orden = importe vigente nuevo (inserted) - importe vigente anterior (deleted).
        //    Insertar lineas => delta positivo; EstadoLinea 1 -> 0 => delta negativo (capacidad devuelta).
        //  - Recalcula TB_Ordenes_Compra.Monto = SUM(Importe) de las lineas vigentes.
        //  - Aplica el delta a MontoComprometido de (YEAR(FechaEmision), BodegaDestino), salvo que la
        //    orden este ANULADA (sus lineas ya no se pueden tocar: THROW 50013).
        //  - Si la bodega/anio no tiene presupuesto, THROW 50010; si el disponible no alcanza, THROW 50012
        //    (se valida antes de sumar, con UPDLOCK). Cualquier THROW deshace toda la transaccion.
        private const string TriggerDetallePresupuesto = @"
CREATE OR ALTER TRIGGER [dbo].[TR_OC_Detalle_Presupuesto]
ON [dbo].[TB_Ordenes_Compra_Detalle]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    DECLARE @Mensaje nvarchar(2048);

    -- decimal(38,2) en los acumulados: una suma grande no desborda aqui (el SP ya acota cada orden).
    DECLARE @Ordenes TABLE (
        [FechaEmision] datetime2(0)     NOT NULL,
        [OrdenId]      uniqueidentifier NOT NULL,
        [Delta]        decimal(38,2)    NOT NULL,
        PRIMARY KEY ([FechaEmision], [OrdenId]));

    INSERT INTO @Ordenes ([FechaEmision], [OrdenId], [Delta])
    SELECT m.[FechaEmision], m.[OrdenId], SUM(m.[Importe])
    FROM (
        SELECT i.[FechaEmision], i.[OrdenId],
               CASE WHEN i.[EstadoLinea] = 1 THEN i.[Importe] ELSE 0 END AS [Importe]
        FROM inserted i
        UNION ALL
        SELECT d.[FechaEmision], d.[OrdenId],
               CASE WHEN d.[EstadoLinea] = 1 THEN -d.[Importe] ELSE 0 END
        FROM deleted d
    ) AS m
    GROUP BY m.[FechaEmision], m.[OrdenId];

    -- Las lineas de una orden ANULADA ya no se tocan (su presupuesto ya fue liberado).
    -- READCOMMITTEDLOCK: lectura con bloqueo aunque la base tenga READ_COMMITTED_SNAPSHOT,
    -- para no aprobar el cambio contra una version vieja del encabezado.
    IF EXISTS (
        SELECT 1
        FROM @Ordenes a
        JOIN [dbo].[TB_Ordenes_Compra] o WITH (READCOMMITTEDLOCK)
            ON o.[FechaEmision] = a.[FechaEmision] AND o.[Id] = a.[OrdenId]
        WHERE o.[Estado] = N'ANULADA')
    BEGIN
        THROW 50013, N'No se pueden modificar lineas de una orden ANULADA', 1;
    END;

    -- Total de la orden = suma de lineas vigentes (el backend nunca lo calcula).
    UPDATE o
    SET o.[Monto] = ISNULL(t.[Total], 0)
    FROM [dbo].[TB_Ordenes_Compra] o
    JOIN @Ordenes a ON o.[FechaEmision] = a.[FechaEmision] AND o.[Id] = a.[OrdenId]
    OUTER APPLY (
        SELECT SUM(l.[Importe]) AS [Total]
        FROM [dbo].[TB_Ordenes_Compra_Detalle] l WITH (READCOMMITTEDLOCK)
        WHERE l.[FechaEmision] = o.[FechaEmision] AND l.[OrdenId] = o.[Id] AND l.[EstadoLinea] = 1
    ) AS t;

    DECLARE @Presupuesto TABLE (
        [Anio]   smallint      NOT NULL,
        [Bodega] nvarchar(60)  COLLATE DATABASE_DEFAULT NOT NULL,
        [Delta]  decimal(38,2) NOT NULL,
        PRIMARY KEY ([Anio], [Bodega]));

    INSERT INTO @Presupuesto ([Anio], [Bodega], [Delta])
    SELECT YEAR(o.[FechaEmision]), o.[BodegaDestino], SUM(a.[Delta])
    FROM @Ordenes a
    JOIN [dbo].[TB_Ordenes_Compra] o ON o.[FechaEmision] = a.[FechaEmision] AND o.[Id] = a.[OrdenId]
    GROUP BY YEAR(o.[FechaEmision]), o.[BodegaDestino]
    HAVING SUM(a.[Delta]) <> 0;

    -- Comprometer contra una bodega/anio sin presupuesto no se permite (el SP ya lo valida;
    -- esto cubre inserciones directas en el detalle).
    IF EXISTS (
        SELECT 1
        FROM @Presupuesto x
        WHERE x.[Delta] > 0
          AND NOT EXISTS (
              SELECT 1 FROM [dbo].[TB_Presupuesto_Bodega] p
              WHERE p.[Anio] = x.[Anio] AND p.[Bodega] = x.[Bodega]))
    BEGIN
        SELECT TOP (1) @Mensaje = CONCAT(N'No existe presupuesto de compras para la bodega ''', x.[Bodega],
               N''' en el anio ', x.[Anio])
        FROM @Presupuesto x
        WHERE x.[Delta] > 0
          AND NOT EXISTS (
              SELECT 1 FROM [dbo].[TB_Presupuesto_Bodega] p
              WHERE p.[Anio] = x.[Anio] AND p.[Bodega] = x.[Bodega]);

        THROW 50010, @Mensaje, 1;
    END;

    -- Capacidad agotada: se valida ANTES de sumar (asi un monto enorme no desborda decimal(18,2))
    -- con UPDLOCK/HOLDLOCK, que retiene la fila hasta el fin de la transaccion y evita que dos
    -- transacciones aprueben a la vez contra el mismo disponible. El THROW deshace todo.
    IF EXISTS (
        SELECT 1
        FROM [dbo].[TB_Presupuesto_Bodega] p WITH (UPDLOCK, HOLDLOCK)
        JOIN @Presupuesto x ON p.[Anio] = x.[Anio] AND p.[Bodega] = x.[Bodega]
        WHERE x.[Delta] > 0 AND p.[MontoDisponible] < x.[Delta])
    BEGIN
        SELECT TOP (1) @Mensaje = CONCAT(N'Presupuesto insuficiente para la bodega ', p.[Bodega],
               N' en ', p.[Anio], N': disponible ', p.[MontoDisponible],
               N', requerido ', x.[Delta])
        FROM [dbo].[TB_Presupuesto_Bodega] p
        JOIN @Presupuesto x ON p.[Anio] = x.[Anio] AND p.[Bodega] = x.[Bodega]
        WHERE x.[Delta] > 0 AND p.[MontoDisponible] < x.[Delta];

        THROW 50012, @Mensaje, 1;
    END;

    UPDATE p
    SET p.[MontoComprometido] = p.[MontoComprometido] + x.[Delta]
    FROM [dbo].[TB_Presupuesto_Bodega] p
    JOIN @Presupuesto x ON p.[Anio] = x.[Anio] AND p.[Bodega] = x.[Bodega];
END;";

        // AFTER UPDATE sobre el encabezado: al pasar a ANULADA devuelve al presupuesto lo que la orden
        // tenia comprometido (la suma de sus lineas vigentes, que es su Monto). Una orden ANULADA no
        // puede volver a otro estado (50021). Ordenes sin lineas (historicas / stress test) liberan 0.
        // Tampoco se permite cambiar BodegaDestino si la orden tiene lineas vigentes (50022).
        private const string TriggerEncabezadoAnulacion = @"
CREATE OR ALTER TRIGGER [dbo].[TR_OC_Encabezado_Anulacion]
ON [dbo].[TB_Ordenes_Compra]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Lo comprometido quedo cargado a (anio, bodega) de la orden: si se cambiara la bodega con
    -- lineas vigentes, la vieja quedaria sobrecomprometida y la nueva liberaria de mas al anular.
    IF UPDATE([BodegaDestino]) AND EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.[FechaEmision] = i.[FechaEmision] AND d.[Id] = i.[Id]
        WHERE i.[BodegaDestino] <> d.[BodegaDestino]
          AND EXISTS (
              SELECT 1 FROM [dbo].[TB_Ordenes_Compra_Detalle] l WITH (READCOMMITTEDLOCK)
              WHERE l.[FechaEmision] = i.[FechaEmision] AND l.[OrdenId] = i.[Id] AND l.[EstadoLinea] = 1))
    BEGIN
        THROW 50022, N'No se puede cambiar la bodega destino de una orden con lineas vigentes', 1;
    END;

    IF NOT UPDATE([Estado]) RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.[FechaEmision] = i.[FechaEmision] AND d.[Id] = i.[Id]
        WHERE d.[Estado] = N'ANULADA' AND i.[Estado] <> N'ANULADA')
    BEGIN
        THROW 50021, N'Una orden ANULADA no puede cambiar de estado', 1;
    END;

    DECLARE @Liberar TABLE (
        [Anio]   smallint      NOT NULL,
        [Bodega] nvarchar(60)  COLLATE DATABASE_DEFAULT NOT NULL,
        [Monto]  decimal(38,2) NOT NULL,
        PRIMARY KEY ([Anio], [Bodega]));

    -- READCOMMITTEDLOCK: suma las lineas vigentes con bloqueo aunque la base use RCSI, para no
    -- liberar un importe que otra transaccion ya devolvio al anular esa linea.
    INSERT INTO @Liberar ([Anio], [Bodega], [Monto])
    SELECT YEAR(i.[FechaEmision]), i.[BodegaDestino], SUM(ISNULL(t.[Total], 0))
    FROM inserted i
    JOIN deleted d ON d.[FechaEmision] = i.[FechaEmision] AND d.[Id] = i.[Id]
    OUTER APPLY (
        SELECT SUM(l.[Importe]) AS [Total]
        FROM [dbo].[TB_Ordenes_Compra_Detalle] l WITH (READCOMMITTEDLOCK)
        WHERE l.[FechaEmision] = i.[FechaEmision] AND l.[OrdenId] = i.[Id] AND l.[EstadoLinea] = 1
    ) AS t
    WHERE i.[Estado] = N'ANULADA' AND d.[Estado] <> N'ANULADA'
    GROUP BY YEAR(i.[FechaEmision]), i.[BodegaDestino]
    HAVING SUM(ISNULL(t.[Total], 0)) <> 0;

    UPDATE p
    SET p.[MontoComprometido] = p.[MontoComprometido] - x.[Monto]
    FROM [dbo].[TB_Presupuesto_Bodega] p
    JOIN @Liberar x ON p.[Anio] = x.[Anio] AND p.[Bodega] = x.[Bodega];
END;";

        private const string TriggerEncabezadoNoDelete = @"
CREATE OR ALTER TRIGGER [dbo].[TR_OC_Encabezado_NoDelete]
ON [dbo].[TB_Ordenes_Compra]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    THROW 50020, N'Borrado fisico prohibido: use Soft Delete', 1;
END;";

        private const string TriggerDetalleNoDelete = @"
CREATE OR ALTER TRIGGER [dbo].[TR_OC_Detalle_NoDelete]
ON [dbo].[TB_Ordenes_Compra_Detalle]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    THROW 50020, N'Borrado fisico prohibido: use Soft Delete', 1;
END;";

        // Recibe la orden completa en JSON y la graba en UNA transaccion: encabezado + N lineas.
        // Si una sola linea falla (validacion, CHECK, presupuesto insuficiente) se hace ROLLBACK
        // absoluto: no queda ni encabezado ni lineas. Devuelve la orden creada con su Monto final.
        private const string SpOrdenCompraCrear = @"
CREATE OR ALTER PROCEDURE [dbo].[SP_OrdenCompra_Crear]
    @Json   nvarchar(max),
    @UserId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Id               uniqueidentifier = NEWID();
    DECLARE @FechaTexto       nvarchar(40);
    DECLARE @FechaEmision     datetime2(0);
    DECLARE @ProveedorNit     nvarchar(4000);
    DECLARE @ProveedorNombre  nvarchar(4000);
    DECLARE @Moneda           nvarchar(4000);
    DECLARE @BodegaTexto      nvarchar(4000);
    DECLARE @Bodega           nvarchar(60);
    DECLARE @Detalle          nvarchar(4000);
    DECLARE @LineasJson       nvarchar(max);
    DECLARE @Anio             smallint;
    DECLARE @Secuencia        int;
    DECLARE @NumeroOrden      nvarchar(30);
    DECLARE @LineaMala        int;
    DECLARE @Mensaje          nvarchar(2048);

    -- CantidadNum / PrecioNum guardan el numero tal como llego (sin redondear) para detectar
    -- decimales de mas o valores fuera de rango, en lugar de redondearlos en silencio.
    DECLARE @Lineas TABLE (
        [Linea]          int             NOT NULL PRIMARY KEY,
        [Tipo]           tinyint         NOT NULL,
        [Descripcion]    nvarchar(4000)  NULL,
        [CantidadNum]    decimal(38,12)  NULL,
        [PrecioNum]      decimal(38,12)  NULL,
        [Cantidad]       decimal(18,3)   NULL,
        [PrecioUnitario] decimal(18,4)   NULL);

    BEGIN TRY
        -- 1. Lectura del encabezado
        IF @Json IS NULL OR ISJSON(@Json) = 0 OR LEFT(LTRIM(@Json), 1) <> N'{'
            THROW 50011, N'El cuerpo de la orden no es un objeto JSON valido', 1;

        IF @UserId IS NULL OR @UserId <= 0
            THROW 50011, N'El usuario que registra la orden es obligatorio', 1;

        SELECT
            @FechaTexto      = h.[fechaEmision],
            @ProveedorNit    = UPPER(LTRIM(RTRIM(h.[proveedorNit]))),
            @ProveedorNombre = LTRIM(RTRIM(h.[proveedorNombre])),
            @Moneda          = UPPER(LTRIM(RTRIM(ISNULL(h.[moneda], N'GTQ')))),
            @BodegaTexto     = LTRIM(RTRIM(h.[bodegaDestino])),
            @Detalle         = NULLIF(LTRIM(RTRIM(h.[detalle])), N'')
        FROM OPENJSON(@Json) WITH (
            [fechaEmision]    nvarchar(40)   '$.fechaEmision',
            [proveedorNit]    nvarchar(4000) '$.proveedorNit',
            [proveedorNombre] nvarchar(4000) '$.proveedorNombre',
            [moneda]          nvarchar(4000) '$.moneda',
            [bodegaDestino]   nvarchar(4000) '$.bodegaDestino',
            [detalle]         nvarchar(4000) '$.detalle'
        ) AS h;

        IF @FechaTexto IS NULL
            SET @FechaEmision = CAST(CAST(SYSUTCDATETIME() AS date) AS datetime2(0));
        ELSE
            SET @FechaEmision = CAST(TRY_CONVERT(date, LEFT(@FechaTexto, 10), 23) AS datetime2(0));

        IF @FechaEmision IS NULL
            THROW 50011, N'fechaEmision debe tener el formato yyyy-MM-dd', 1;
        IF @ProveedorNit IS NULL OR @ProveedorNit = N'' OR LEN(@ProveedorNit) > 20
            THROW 50011, N'proveedorNit es obligatorio (maximo 20 caracteres)', 1;
        IF @ProveedorNombre IS NULL OR @ProveedorNombre = N'' OR LEN(@ProveedorNombre) > 150
            THROW 50011, N'proveedorNombre es obligatorio (maximo 150 caracteres)', 1;
        IF LEN(@Moneda) <> 3
            THROW 50011, N'moneda debe ser un codigo ISO de 3 letras', 1;
        IF @BodegaTexto IS NULL OR @BodegaTexto = N'' OR LEN(@BodegaTexto) > 60
            THROW 50011, N'bodegaDestino es obligatoria (maximo 60 caracteres)', 1;
        IF LEN(@Detalle) > 255
            THROW 50011, N'detalle admite maximo 255 caracteres', 1;

        -- 2. Lectura de las lineas (numeradas 1..N en el orden del arreglo)
        SET @LineasJson = JSON_QUERY(@Json, '$.lineas');

        IF @LineasJson IS NOT NULL AND LEFT(LTRIM(@LineasJson), 1) = N'['
        BEGIN
            -- JSON_VALUE solo sobre elementos objeto (type 5): con un escalar (texto, numero) lanzaria 13609.
            INSERT INTO @Lineas ([Linea], [Tipo], [Descripcion], [CantidadNum], [PrecioNum])
            SELECT
                ROW_NUMBER() OVER (ORDER BY CAST(j.[key] AS int)),
                j.[type],
                CASE WHEN j.[type] = 5 THEN LTRIM(RTRIM(JSON_VALUE(j.[value], '$.descripcion'))) END,
                CASE WHEN j.[type] = 5 THEN TRY_CAST(JSON_VALUE(j.[value], '$.cantidad') AS decimal(38,12)) END,
                CASE WHEN j.[type] = 5 THEN TRY_CAST(JSON_VALUE(j.[value], '$.precioUnitario') AS decimal(38,12)) END
            FROM OPENJSON(@LineasJson) AS j;

            -- Conversion a la escala de las columnas; si redondea o desborda se rechaza abajo.
            UPDATE @Lineas
            SET [Cantidad]       = TRY_CAST([CantidadNum] AS decimal(18,3)),
                [PrecioUnitario] = TRY_CAST([PrecioNum] AS decimal(18,4));
        END;

        BEGIN TRAN;

        -- 3. Debe existir presupuesto para (anio, bodega). UPDLOCK serializa a quienes compran
        --    contra la misma bodega mientras dura la transaccion.
        SET @Anio = YEAR(@FechaEmision);

        SELECT @Bodega = p.[Bodega]
        FROM [dbo].[TB_Presupuesto_Bodega] p WITH (UPDLOCK, HOLDLOCK)
        WHERE p.[Anio] = @Anio AND p.[Bodega] = @BodegaTexto;

        IF @Bodega IS NULL
        BEGIN
            SET @Mensaje = CONCAT(N'No existe presupuesto de compras para la bodega ''', @BodegaTexto,
                                  N''' en el anio ', @Anio);
            THROW 50010, @Mensaje, 1;
        END;

        -- 4. Encabezado: nace CREADA y con Monto 0 (el trigger del detalle lo calcula).
        SET @Secuencia = NEXT VALUE FOR [dbo].[SEQ_OrdenCompra_Numero];
        SET @NumeroOrden = CONCAT(N'OC-', @Anio, N'-', RIGHT(CONCAT(N'000000', @Secuencia), CASE WHEN @Secuencia > 999999 THEN LEN(@Secuencia) ELSE 6 END));

        INSERT INTO [dbo].[TB_Ordenes_Compra]
            ([Id], [FechaEmision], [NumeroOrden], [UserId], [ProveedorNit], [ProveedorNombre],
             [Monto], [Moneda], [Estado], [BodegaDestino], [Detalle], [CreatedAt])
        VALUES
            (@Id, @FechaEmision, @NumeroOrden, @UserId, @ProveedorNit, @ProveedorNombre,
             0, @Moneda, N'CREADA', @Bodega, @Detalle, SYSUTCDATETIME());

        -- 5. Lineas: si una sola es invalida se aborta todo (el encabezado ya insertado incluido).
        IF NOT EXISTS (SELECT 1 FROM @Lineas)
            THROW 50011, N'La orden debe traer al menos una linea en ''lineas''', 1;

        SELECT TOP (1)
            @LineaMala = l.[Linea],
            @Mensaje = CONCAT(N'Linea ', l.[Linea], N' invalida: ',
                CASE
                    WHEN l.[Tipo] <> 5 THEN N'cada linea debe ser un objeto JSON'
                    WHEN l.[Descripcion] IS NULL OR l.[Descripcion] = N'' THEN N'descripcion es obligatoria'
                    WHEN LEN(l.[Descripcion]) > 150 THEN N'descripcion admite maximo 150 caracteres'
                    WHEN l.[CantidadNum] IS NULL OR l.[CantidadNum] <= 0 THEN N'cantidad debe ser un numero mayor que 0'
                    WHEN l.[Cantidad] IS NULL OR l.[Cantidad] <> l.[CantidadNum] THEN N'cantidad admite maximo 15 enteros y 3 decimales'
                    WHEN l.[PrecioNum] IS NULL OR l.[PrecioNum] < 0 THEN N'precioUnitario debe ser un numero mayor o igual a 0'
                    WHEN l.[PrecioUnitario] IS NULL OR l.[PrecioUnitario] <> l.[PrecioNum] THEN N'precioUnitario admite maximo 14 enteros y 4 decimales'
                    ELSE N'el importe (cantidad x precioUnitario) excede el maximo de 9,999,999,999,999,999.99'
                END)
        FROM @Lineas l
        WHERE l.[Tipo] <> 5
           OR l.[Descripcion] IS NULL OR l.[Descripcion] = N'' OR LEN(l.[Descripcion]) > 150
           OR l.[CantidadNum] IS NULL OR l.[CantidadNum] <= 0
           OR l.[Cantidad] IS NULL OR l.[Cantidad] <> l.[CantidadNum]
           OR l.[PrecioNum] IS NULL OR l.[PrecioNum] < 0
           OR l.[PrecioUnitario] IS NULL OR l.[PrecioUnitario] <> l.[PrecioNum]
           OR ROUND(l.[Cantidad] * l.[PrecioUnitario], 2) > 9999999999999999.99
        ORDER BY l.[Linea];

        IF @LineaMala IS NOT NULL
            THROW 50011, @Mensaje, 1;

        -- Importe y Monto son decimal(18,2): se rechaza antes de que el INSERT desborde (8115).
        IF (SELECT SUM(ROUND(l.[Cantidad] * l.[PrecioUnitario], 2)) FROM @Lineas l) > 9999999999999999.99
            THROW 50011, N'El total de la orden excede el maximo de 9,999,999,999,999,999.99', 1;

        -- El CHECK de Cantidad/PrecioUnitario y el trigger de presupuesto son la ultima defensa.
        INSERT INTO [dbo].[TB_Ordenes_Compra_Detalle]
            ([FechaEmision], [OrdenId], [Linea], [Descripcion], [Cantidad], [PrecioUnitario], [EstadoLinea])
        SELECT @FechaEmision, @Id, l.[Linea], l.[Descripcion], l.[Cantidad], l.[PrecioUnitario], 1
        FROM @Lineas l;

        COMMIT TRAN;

        SELECT o.[Id], o.[FechaEmision], o.[NumeroOrden], o.[Monto], o.[Estado], o.[RowVer]
        FROM [dbo].[TB_Ordenes_Compra] o
        WHERE o.[FechaEmision] = @FechaEmision AND o.[Id] = @Id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRAN;
        THROW;
    END CATCH;
END;";
    }
}
