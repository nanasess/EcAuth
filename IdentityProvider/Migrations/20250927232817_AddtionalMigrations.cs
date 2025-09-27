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
            migrationBuilder.DropPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "ecauth_user",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ecauth_user_subject",
                table: "ecauth_user",
                column: "subject");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_subject",
                table: "ecauth_user",
                column: "subject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_ecauth_user_subject",
                table: "ecauth_user");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user");

            migrationBuilder.DropIndex(
                name: "IX_ecauth_user_subject",
                table: "ecauth_user");

            migrationBuilder.DropColumn(
                name: "id",
                table: "ecauth_user");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user",
                column: "subject");
        }
    }
}
