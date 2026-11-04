using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seminario_backend_service.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Fase 3: ranking de proveedores calculado por SQL Server. dbo.FN_Ranking_Proveedores(@Anio) es una
    /// funcion en linea que suma el Importe de las lineas vigentes de las ordenes no anuladas del anio y
    /// numera con RANK() OVER (ORDER BY SUM(Importe) DESC). El backend solo la consulta, no ordena en memoria.
    /// No cambia el modelo de EF (solo SQL nativo). Equivalente manual: sql/06_fase3_ranking_reportes.sql.
    /// </summary>
    public partial class Fase3ReportesRanking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CREATE OR ALTER FUNCTION debe ser el unico statement del batch: va en su propio Sql().
            migrationBuilder.Sql(@"
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
);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS [dbo].[FN_Ranking_Proveedores];");
        }
    }
}
