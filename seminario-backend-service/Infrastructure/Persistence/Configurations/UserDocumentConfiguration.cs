using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Persistence.Configurations;

public class UserDocumentConfiguration : IEntityTypeConfiguration<UserDocument>
{
    public void Configure(EntityTypeBuilder<UserDocument> builder)
    {
        builder.ToTable("UserDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();

        // Bloqueo optimista: columna rowversion administrada por SQL Server.
        builder.Property(d => d.RowVersion).IsRowVersion();

        // Soft Delete: toda consulta (incluido el Include de User.Documents) ignora los eliminados.
        builder.Property(d => d.IsDeleted).HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnType("datetime2(0)");
        builder.HasQueryFilter(d => !d.IsDeleted);

        // Restrict (NO ACTION) en vez de Cascade: SQL Server no permite un trigger INSTEAD OF DELETE
        // sobre una tabla con FK en cascada, y los usuarios tampoco se borran fisicamente.
        builder.HasOne(d => d.User)
            .WithMany(u => u.Documents)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // El INSTEAD OF DELETE que prohibe el borrado fisico lo crea la migracion con SQL nativo;
        // declararlo aqui hace que EF no use OUTPUT sin INTO en los UPDATE de esta tabla.
        builder.ToTable(t => t.HasTrigger("TR_UserDocuments_NoDelete"));
    }
}
