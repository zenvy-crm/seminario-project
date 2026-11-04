using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Persistence.Configurations;

public class OrdenCompraConfiguration : IEntityTypeConfiguration<OrdenCompra>
{
    public void Configure(EntityTypeBuilder<OrdenCompra> builder)
    {
        // HasTrigger: la tabla tiene triggers, asi que EF no puede usar OUTPUT sin INTO al
        // guardar (SQL Server lo rechaza). Declararlos hace que EF cambie de estrategia.
        builder.ToTable(OrdenCompraSqlObjects.TablaEncabezado, tb =>
        {
            tb.HasTrigger(OrdenCompraSqlObjects.TriggerEncabezadoAnulacion);
            tb.HasTrigger(OrdenCompraSqlObjects.TriggerEncabezadoNoDelete);
        });

        builder.HasKey(o => new { o.FechaEmision, o.Id });

        builder.Property(o => o.FechaEmision).HasColumnType("datetime2(0)");
        builder.Property(o => o.NumeroOrden).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ProveedorNit).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ProveedorNombre).HasMaxLength(150).IsRequired();
        builder.Property(o => o.Monto).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Moneda).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Estado).HasMaxLength(20).IsRequired();
        builder.Property(o => o.BodegaDestino).HasMaxLength(60).IsRequired();
        builder.Property(o => o.Detalle).HasMaxLength(255);
        builder.Property(o => o.CreatedAt).HasColumnType("datetime2(0)");

        // Soft delete: columnas NULL para que el stress test (SqlBulkCopy de 12 columnas) siga funcionando.
        builder.Property(o => o.FechaAnulacion).HasColumnType("datetime2(0)");
        builder.Property(o => o.MotivoAnulacion).HasMaxLength(200);

        builder.Property(o => o.RowVer).IsRowVersion();

        // Indices alineados con la particion: incluyen la columna de particion para
        // que SQL Server los cree particionados y no como un indice global.
        builder.HasIndex(o => new { o.FechaEmision, o.UserId }).HasDatabaseName("IX_Ordenes_Fecha_Usuario");
        builder.HasIndex(o => new { o.FechaEmision, o.ProveedorNit }).HasDatabaseName("IX_Ordenes_Fecha_Proveedor");

        // Busqueda por Id sin conocer la fecha (GET/PUT/DELETE /ordenes/{id}): indice alineado,
        // SQL Server hace un seek por particion en lugar de recorrer la tabla completa.
        builder.HasIndex(o => o.Id).HasDatabaseName("IX_Ordenes_Id");

        // Sin FK a Users: una FK sobre una tabla particionada de millones de filas
        // penaliza la carga masiva. La integridad se valida en el servicio.
        builder.Ignore(o => o.User);

        builder.HasMany(o => o.Lineas)
            .WithOne(l => l.Orden)
            .HasForeignKey(l => new { l.FechaEmision, l.OrdenId })
            .HasPrincipalKey(o => new { o.FechaEmision, o.Id })
            .HasConstraintName("FK_OC_Detalle_Encabezado")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
