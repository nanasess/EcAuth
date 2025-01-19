using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class addIdP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "open_id_provider",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    client_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    client_secret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    discovery_document_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    issuer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    authorization_endpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    token_endpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    userinfo_endpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    jwks_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_open_id_provider", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "open_id_provider_scope",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    open_id_provider_id = table.Column<int>(type: "int", nullable: false),
                    scope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_open_id_provider_scope", x => x.Id);
                    table.ForeignKey(
                        name: "FK_open_id_provider_scope_open_id_provider_open_id_provider_id",
                        column: x => x.open_id_provider_id,
                        principalTable: "open_id_provider",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_open_id_provider_scope_open_id_provider_id",
                table: "open_id_provider_scope",
                column: "open_id_provider_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "open_id_provider_scope");

            migrationBuilder.DropTable(
                name: "open_id_provider");
        }
    }
}
