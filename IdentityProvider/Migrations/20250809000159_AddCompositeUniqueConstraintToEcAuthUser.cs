using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeUniqueConstraintToEcAuthUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ecauth_user_email_hash",
                table: "ecauth_user");

            migrationBuilder.DropIndex(
                name: "IX_ecauth_user_organization_id",
                table: "ecauth_user");

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_organization_id_email_hash",
                table: "ecauth_user",
                columns: new[] { "organization_id", "email_hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ecauth_user_organization_id_email_hash",
                table: "ecauth_user");

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_email_hash",
                table: "ecauth_user",
                column: "email_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_organization_id",
                table: "ecauth_user",
                column: "organization_id");
        }
    }
}
