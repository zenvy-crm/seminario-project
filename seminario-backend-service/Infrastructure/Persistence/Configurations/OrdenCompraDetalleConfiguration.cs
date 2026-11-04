using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Persistence.Configurations;

/// <summary>
/// Detalle de la orden de compra. La tabla real la crea la migracion con SQL nativo sobre
/// PS_RangoFechasOrdenes([FechaEmision]); esta configuracion solo describe el modelo para EF.
/// </summary>
public class OrdenCompraDetalleConfiguration : IEntityTypeConfiguration<OrdenCompraDetalle>
{
    public void Configure(EntityTypeBuilder<OrdenCompraDetalle> builder)
    {
        builder.ToTable(OrdenCompraSqlObjects.TablaDetalle, tb =>
        {
            tb.HasTrigger(OrdenCompraSqlObjects.TriggerDetallePresupuesto);
            tb.HasTrigger(OrdenCompraSqlObjects.TriggerDetalleNoDelete);
            tb.HasCheckConstraint("CK_OC_Detalle_Cantidad", "[Cantidad] > 0");
            tb.HasCheckConstraint("CK_OC_Detalle_Precio", "[PrecioUnitario] >= 0");
        });

        // Llave clusterizada con la columna de particion al frente (alineada con el encabezado).
        builder.HasKey(l => new { l.FechaEmision, l.OrdenId, l.Linea });

        builder.Property(l => l.FechaEmision).HasColumnType("datetime2(0)");
        builder.Property(l => l.Descripcion).HasMaxLength(150).IsRequired();
        builder.Property(l => l.Cantidad).HasColumnType("decimal(18,3)");
        builder.Property(l => l.PrecioUnitario).HasColumnType("decimal(18,4)");

        builder.Property(l => l.Importe)
            .HasColumnType("decimal(18,2)")
            .HasComputedColumnSql("CAST(ROUND([Cantidad] * [PrecioUnitario], 2) AS decimal(18,2))", stored: true);

        // DEFAULT 1 vive en la base de datos; el valor inicial true de la entidad se envia siempre.
        builder.Property(l => l.EstadoLinea).IsRequired();

        builder.Property(l => l.FechaBaja).HasColumnType("datetime2(0)");
        builder.Property(l => l.MotivoBaja).HasMaxLength(200);

        builder.Property(l => l.RowVer).IsRowVersion();
    }
}
