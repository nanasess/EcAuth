using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddtionalMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_access_token_ecauth_user_ecauth_subject",
                table: "access_token");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ecauth_user_TempId",
                table: "ecauth_user");

            migrationBuilder.DropColumn(
                name: "TempId",
                table: "ecauth_user");

            migrationBuilder.AddForeignKey(
                name: "FK_access_token_ecauth_user_ecauth_subject",
                table: "access_token",
                column: "ecauth_subject",
                principalTable: "ecauth_user",
                principalColumn: "subject",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_access_token_ecauth_user_ecauth_subject",
                table: "access_token");

            migrationBuilder.AddColumn<string>(
                name: "TempId",
                table: "ecauth_user",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ecauth_user_TempId",
                table: "ecauth_user",
                column: "TempId");

            migrationBuilder.AddForeignKey(
                name: "FK_access_token_ecauth_user_ecauth_subject",
                table: "access_token",
                column: "ecauth_subject",
                principalTable: "ecauth_user",
                principalColumn: "TempId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
