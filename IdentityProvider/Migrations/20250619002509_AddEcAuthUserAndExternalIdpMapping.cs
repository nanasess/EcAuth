using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddEcAuthUserAndExternalIdpMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ecauth_user",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ecauth_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_ecauth_user_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_idp_mapping",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ecauth_user_id = table.Column<int>(type: "int", nullable: false),
                    external_provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    external_subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_idp_mapping", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_idp_mapping_ecauth_user_ecauth_user_id",
                        column: x => x.ecauth_user_id,
                        principalTable: "ecauth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_organization_id",
                table: "ecauth_user",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_idp_mapping_ecauth_user_id",
                table: "external_idp_mapping",
                column: "ecauth_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_idp_mapping");

            migrationBuilder.DropTable(
                name: "ecauth_user");
        }
    }
}
