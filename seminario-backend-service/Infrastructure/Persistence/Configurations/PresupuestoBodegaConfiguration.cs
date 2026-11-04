using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Persistence.Configurations;

/// <summary>
/// Presupuesto de compras por (Anio, Bodega). MontoComprometido solo lo tocan los triggers;
/// el backend lo trata como lectura.
/// </summary>
public class PresupuestoBodegaConfiguration : IEntityTypeConfiguration<PresupuestoBodega>
{
    public void Configure(EntityTypeBuilder<PresupuestoBodega> builder)
    {
        builder.ToTable(OrdenCompraSqlObjects.TablaPresupuesto);

        builder.HasKey(p => new { p.Anio, p.Bodega });

        builder.Property(p => p.Bodega).HasMaxLength(60);
        builder.Property(p => p.MontoAsignado).HasColumnType("decimal(18,2)");
        builder.Property(p => p.MontoComprometido).HasColumnType("decimal(18,2)");

        builder.Property(p => p.MontoDisponible)
            .HasColumnType("decimal(18,2)")
            .HasComputedColumnSql("CAST([MontoAsignado] - [MontoComprometido] AS decimal(18,2))", stored: true);

        builder.Property(p => p.RowVer).IsRowVersion();
    }
}
