using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class FixIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "open_id_provider_scope",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "open_id_provider",
                newName: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "open_id_provider_scope",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "open_id_provider",
                newName: "Id");
        }
    }
}
