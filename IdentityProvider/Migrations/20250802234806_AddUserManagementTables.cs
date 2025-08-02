using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "client",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_client_client_id",
                table: "client",
                column: "client_id");

            migrationBuilder.CreateTable(
                name: "ecauth_user",
                columns: table => new
                {
                    subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ecauth_user", x => x.subject);
                    table.ForeignKey(
                        name: "FK_ecauth_user_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "authorization_code",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ecauth_subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    client_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    redirect_uri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    state = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_code", x => x.code);
                    table.ForeignKey(
                        name: "FK_authorization_code_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_authorization_code_ecauth_user_ecauth_subject",
                        column: x => x.ecauth_subject,
                        principalTable: "ecauth_user",
                        principalColumn: "subject",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_idp_mapping",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ecauth_subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    external_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    external_subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_idp_mapping", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_idp_mapping_ecauth_user_ecauth_subject",
                        column: x => x.ecauth_subject,
                        principalTable: "ecauth_user",
                        principalColumn: "subject",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_client_id",
                table: "authorization_code",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_ecauth_subject",
                table: "authorization_code",
                column: "ecauth_subject");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_expires_at",
                table: "authorization_code",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_email_hash",
                table: "ecauth_user",
                column: "email_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_organization_id",
                table: "ecauth_user",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_idp_mapping_ecauth_subject",
                table: "external_idp_mapping",
                column: "ecauth_subject");

            migrationBuilder.CreateIndex(
                name: "IX_external_idp_mapping_external_provider_external_subject",
                table: "external_idp_mapping",
                columns: new[] { "external_provider", "external_subject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_code");

            migrationBuilder.DropTable(
                name: "external_idp_mapping");

            migrationBuilder.DropTable(
                name: "ecauth_user");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_client_client_id",
                table: "client");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "client",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
