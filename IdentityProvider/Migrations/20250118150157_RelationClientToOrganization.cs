using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class RelationClientToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "organization_id",
                table: "client",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_organization_id",
                table: "client",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "FK_client_organization_organization_id",
                table: "client",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_client_organization_organization_id",
                table: "client");

            migrationBuilder.DropIndex(
                name: "IX_client_organization_id",
                table: "client");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "client");
        }
    }
}
