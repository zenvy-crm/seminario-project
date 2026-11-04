using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seminario_backend_service.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Presupuesto para las 5 bodegas que usan el formulario del frontend y el stress test
    /// (SeedDataService). Sin estas filas el SP rechaza las ordenes con 50010 (422).
    /// </summary>
    public partial class SeedPresupuestoBodegasFrontend : Migration
    {
        private const string Bodegas = @"
(N'Bodega Central Zona 12'),
(N'Centro de Distribucion Villa Nueva'),
(N'Bodega Norte Chimaltenango'),
(N'Bodega Regional Quetzaltenango'),
(N'Cross-dock Puerto Barrios')";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mismo monto y anios que la semilla de GT-CENTRO/NORTE/SUR (Fase2OrdenCompraDetalle). Idempotente.
            migrationBuilder.Sql($@"
INSERT INTO [dbo].[TB_Presupuesto_Bodega] ([Anio], [Bodega], [MontoAsignado])
SELECT a.[Anio], b.[Bodega], 2500000.00
FROM (VALUES (2024), (2025), (2026), (2027)) AS a([Anio])
CROSS JOIN (VALUES {Bodegas}) AS b([Bodega])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[TB_Presupuesto_Bodega] p
    WHERE p.[Anio] = a.[Anio] AND p.[Bodega] = b.[Bodega]);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Solo se retiran las filas sin nada comprometido: las demas respaldan ordenes vivas.
            migrationBuilder.Sql($@"
DELETE p
FROM [dbo].[TB_Presupuesto_Bodega] p
JOIN (VALUES {Bodegas}) AS b([Bodega]) ON p.[Bodega] = b.[Bodega]
WHERE p.[Anio] BETWEEN 2024 AND 2027 AND p.[MontoComprometido] = 0;");
        }
    }
}
