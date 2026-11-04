using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seminario_backend_service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserDocumentSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDocuments_Users_UserId",
                table: "UserDocuments");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserDocuments",
                type: "datetime2(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "UserDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDocuments_Users_UserId",
                table: "UserDocuments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Borrado fisico prohibido, igual que en las tablas de ordenes. Va despues del cambio
            // de la FK: SQL Server no admite INSTEAD OF DELETE si la tabla tiene una FK en cascada.
            migrationBuilder.Sql(TriggerNoDelete);
        }

        private const string TriggerNoDelete = @"
CREATE OR ALTER TRIGGER [dbo].[TR_UserDocuments_NoDelete]
ON [dbo].[UserDocuments]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    THROW 50020, N'Borrado fisico prohibido: use Soft Delete', 1;
END;";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Primero el trigger: la FK en cascada que se restaura abajo no puede convivir con el.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_UserDocuments_NoDelete];");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDocuments_Users_UserId",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserDocuments");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDocuments_Users_UserId",
                table: "UserDocuments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
