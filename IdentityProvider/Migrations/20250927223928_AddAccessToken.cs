using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_token",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    client_id = table.Column<int>(type: "int", nullable: false),
                    ecauth_subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    scopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_access_token_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_token_ecauth_user_ecauth_subject",
                        column: x => x.ecauth_subject,
                        principalTable: "ecauth_user",
                        principalColumn: "subject",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_token_client_id",
                table: "access_token",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_ecauth_subject",
                table: "access_token",
                column: "ecauth_subject");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_expires_at",
                table: "access_token",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_token",
                table: "access_token",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_token");
        }
    }
}
