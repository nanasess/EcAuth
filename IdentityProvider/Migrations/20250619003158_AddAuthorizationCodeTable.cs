using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationCodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authorization_code",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    client_id = table.Column<int>(type: "int", nullable: false),
                    ecauth_user_id = table.Column<int>(type: "int", nullable: false),
                    redirect_uri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    used = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_code", x => x.id);
                    table.ForeignKey(
                        name: "FK_authorization_code_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_authorization_code_ecauth_user_ecauth_user_id",
                        column: x => x.ecauth_user_id,
                        principalTable: "ecauth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_client_id",
                table: "authorization_code",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_ecauth_user_id",
                table: "authorization_code",
                column: "ecauth_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_code");
        }
    }
}
