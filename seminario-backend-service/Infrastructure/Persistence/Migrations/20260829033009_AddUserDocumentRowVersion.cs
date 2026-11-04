using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seminario_backend_service.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Agrega el token de concurrencia optimista a UserDocuments.
    ///
    /// SQL Server incrementa la columna rowversion en cada UPDATE de la fila. EF Core la
    /// incluye en el WHERE al actualizar o eliminar, de modo que si otro proceso toco la
    /// fila primero se afectan cero filas y EF lanza DbUpdateConcurrencyException en lugar
    /// de sobrescribir en silencio. Es bloqueo optimista: no bloquea filas.
    /// </summary>
    public partial class AddUserDocumentRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserDocuments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserDocuments");
        }
    }
}
