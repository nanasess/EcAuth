using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class FixIdpRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "client_secret",
                table: "open_id_provider",
                newName: "idp_client_secret");

            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "open_id_provider",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "idp_client_id",
                table: "open_id_provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_open_id_provider_client_id",
                table: "open_id_provider",
                column: "client_id");

            migrationBuilder.AddForeignKey(
                name: "FK_open_id_provider_client_client_id",
                table: "open_id_provider",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_open_id_provider_client_client_id",
                table: "open_id_provider");

            migrationBuilder.DropIndex(
                name: "IX_open_id_provider_client_id",
                table: "open_id_provider");

            migrationBuilder.DropColumn(
                name: "idp_client_id",
                table: "open_id_provider");

            migrationBuilder.RenameColumn(
                name: "idp_client_secret",
                table: "open_id_provider",
                newName: "client_secret");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "open_id_provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
