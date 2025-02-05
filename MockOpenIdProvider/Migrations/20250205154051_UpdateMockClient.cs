using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockOpenIdProvider.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMockClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_access_token_client_ClientId",
                table: "access_token");

            migrationBuilder.DropForeignKey(
                name: "FK_authorization_code_client_ClientId",
                table: "authorization_code");

            migrationBuilder.DropIndex(
                name: "IX_authorization_code_ClientId",
                table: "authorization_code");

            migrationBuilder.DropIndex(
                name: "IX_access_token_ClientId",
                table: "access_token");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "authorization_code");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "access_token");

            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "authorization_code",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "access_token",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_client_id",
                table: "authorization_code",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_client_id",
                table: "access_token",
                column: "client_id");

            migrationBuilder.AddForeignKey(
                name: "FK_access_token_client_client_id",
                table: "access_token",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_authorization_code_client_client_id",
                table: "authorization_code",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_access_token_client_client_id",
                table: "access_token");

            migrationBuilder.DropForeignKey(
                name: "FK_authorization_code_client_client_id",
                table: "authorization_code");

            migrationBuilder.DropIndex(
                name: "IX_authorization_code_client_id",
                table: "authorization_code");

            migrationBuilder.DropIndex(
                name: "IX_access_token_client_id",
                table: "access_token");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "authorization_code",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "authorization_code",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "access_token",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "access_token",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_authorization_code_ClientId",
                table: "authorization_code",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_ClientId",
                table: "access_token",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_access_token_client_ClientId",
                table: "access_token",
                column: "ClientId",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_authorization_code_client_ClientId",
                table: "authorization_code",
                column: "ClientId",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
