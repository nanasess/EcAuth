using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Cryptography;

#nullable disable

namespace MockOpenIdProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddMockClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "private_key",
                table: "client",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "public_key",
                table: "client",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            using RSA rsa = RSA.Create();
            var privateKey = rsa.ExportRSAPrivateKeyPem();
            var publicKey = rsa.ExportRSAPublicKeyPem();

            migrationBuilder.Sql($"INSERT INTO client " +
                $"(client_id, client_secret, client_name, redirect_uri, public_key, private_key) " +
                $"VALUES ('mockclientid', 'mock-client-secret', 'MockClient', 'https://localhost:8081/auth/callback', '{publicKey}', '{privateKey}');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "private_key",
                table: "client");

            migrationBuilder.DropColumn(
                name: "public_key",
                table: "client");
            migrationBuilder.Sql(@"
                DELETE FROM client WHERE client_id = 'mockclientid';");
        }
    }
}
