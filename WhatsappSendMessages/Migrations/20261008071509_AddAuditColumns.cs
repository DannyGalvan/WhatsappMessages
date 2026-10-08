using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappSendMessages.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // === MessagesTemplate: no tenia ninguna columna de auditoria.
            // Backfill con 'legacy' para by y la fecha del deploy para at: las
            // filas historicas no tienen fecha real de creacion, asi que no
            // tiene sentido fingirla.
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MessagesTemplate",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MessagesTemplate",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MessagesTemplate",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MessagesTemplate",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "legacy");

            // === ApiKeys: ya tenia CreatedAt, agregamos CreatedBy/UpdatedAt/UpdatedBy.
            // UpdatedAt se backfilea desde RevokedAt (si la key fue revocada) o
            // desde CreatedAt. Asi no se pierde la unica senial temporal que
            // tenemos para filas historicas.
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ApiKeys",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "legacy");

            // 1) Agregar UpdatedAt como NULL para poder hacer el UPDATE
            //    con COALESCE(RevokedAt, CreatedAt).
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ApiKeys",
                type: "datetime2",
                nullable: true);

            // 2) Backfill.
            migrationBuilder.Sql(
                "UPDATE [ApiKeys] SET [UpdatedAt] = COALESCE([RevokedAt], [CreatedAt])");

            // 3) Alter a NOT NULL ahora que todas las filas tienen valor.
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ApiKeys",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ApiKeys",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "legacy");

            // === WhatsAppAccessTokens: ya tenia UpdatedAt, agregamos CreatedAt/CreatedBy/UpdatedBy.
            // CreatedAt se backfilea desde UpdatedAt (la mejor aproximacion
            // que tenemos para filas previas a esta migracion).
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WhatsAppAccessTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [WhatsAppAccessTokens] SET [CreatedAt] = [UpdatedAt]");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WhatsAppAccessTokens",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "WhatsAppAccessTokens",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "WhatsAppAccessTokens",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "legacy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "WhatsAppAccessTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "WhatsAppAccessTokens");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WhatsAppAccessTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MessagesTemplate");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MessagesTemplate");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MessagesTemplate");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MessagesTemplate");
        }
    }
}
