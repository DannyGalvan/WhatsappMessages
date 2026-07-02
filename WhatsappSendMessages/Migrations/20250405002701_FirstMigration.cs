using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsappSendMessages.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessagesTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessagingProduct = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactInput = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WaId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MessageStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageTemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesTemplate", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagesTemplate");
        }
    }
}
