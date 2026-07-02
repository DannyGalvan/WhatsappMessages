using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappSendMessages.Migrations
{
    /// <inheritdoc />
    public partial class SeedExistingProductionApiKey : Migration
    {
        // Hash SHA-256 (hex, mayusculas) de la API key que ya tenia el cliente configurada
        // en produccion via appsettings.Production.json. Se preserva aqui para que el
        // despliegue no invalide su key actual; el valor en claro nunca se guarda.
        private const string ExistingProductionKeyHash =
            "20712E64D7ED63CEE77B2AF781D956F7E36820231C5485B7839C688A37B24FA6";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [ApiKeys] WHERE [KeyHash] = '{ExistingProductionKeyHash}')
BEGIN
    INSERT INTO [ApiKeys] ([Name], [KeyHash], [IsAdmin], [IsActive], [CreatedAt], [ExpiresAt], [RevokedAt])
    VALUES ('legacy-production-client', '{ExistingProductionKeyHash}', 0, 1, SYSUTCDATETIME(), NULL, NULL);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"DELETE FROM [ApiKeys] WHERE [KeyHash] = '{ExistingProductionKeyHash}';");
        }
    }
}
