using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seminario_backend_service.Infrastructure.Persistence.Migrations
{

    public partial class AddOrdenesCompraParticionada : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PASO 1: Funcion de particion. RANGE RIGHT => el limite
            // pertenece a la particion de la derecha, es decir al anio que inicia.
            migrationBuilder.Sql(@"
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
END;");

            // PASO 2: Esquema de particion. En produccion cada anio iria a un
            // filegroup sobre disco distinto; aqui todos apuntan a PRIMARY.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'PS_RangoFechasOrdenes')
BEGIN
    CREATE PARTITION SCHEME PS_RangoFechasOrdenes
    AS PARTITION FN_RangoFechasOrdenes
    ALL TO ([PRIMARY]);
END;");

            // PASO 3: Tabla particionada. La llave clusterizada incluye la columna de
            // particion y la tabla se ancla al esquema.
            migrationBuilder.Sql(@"
CREATE TABLE [TB_Ordenes_Compra] (
    [Id]               uniqueidentifier NOT NULL DEFAULT NEWID(),
    [FechaEmision]     datetime2(0)     NOT NULL,
    [NumeroOrden]      nvarchar(30)     NOT NULL,
    [UserId]           int              NOT NULL,
    [ProveedorNit]     nvarchar(20)     NOT NULL,
    [ProveedorNombre]  nvarchar(150)    NOT NULL,
    [Monto]            decimal(18,2)    NOT NULL,
    [Moneda]           nvarchar(3)      NOT NULL,
    [Estado]           nvarchar(20)     NOT NULL,
    [BodegaDestino]    nvarchar(60)     NOT NULL,
    [Detalle]          nvarchar(255)    NULL,
    [CreatedAt]        datetime2(0)     NOT NULL,
    CONSTRAINT [PK_TB_Ordenes_Compra] PRIMARY KEY CLUSTERED ([FechaEmision], [Id])
) ON PS_RangoFechasOrdenes([FechaEmision]);");

            // Indices alineados: al crearse sobre el mismo esquema, SQL Server los
            // particiona igual que la tabla y conserva la eliminacion de particiones.
            migrationBuilder.Sql(@"
CREATE NONCLUSTERED INDEX [IX_Ordenes_Fecha_Usuario]
    ON [TB_Ordenes_Compra] ([FechaEmision], [UserId])
    ON PS_RangoFechasOrdenes([FechaEmision]);");

            migrationBuilder.Sql(@"
CREATE NONCLUSTERED INDEX [IX_Ordenes_Fecha_Proveedor]
    ON [TB_Ordenes_Compra] ([FechaEmision], [ProveedorNit])
    ON PS_RangoFechasOrdenes([FechaEmision]);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [TB_Ordenes_Compra];");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'PS_RangoFechasOrdenes')
    DROP PARTITION SCHEME PS_RangoFechasOrdenes;");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'FN_RangoFechasOrdenes')
    DROP PARTITION FUNCTION FN_RangoFechasOrdenes;");
        }
    }
}
