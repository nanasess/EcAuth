using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Cryptography;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertTestRsaKeyPairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Generate a real RSA key pair for testing (2048-bit)
            using var rsa = RSA.Create(2048);

            // Export as raw bytes and convert to Base64 (as expected by TokenService)
            var publicKeyBytes = rsa.ExportRSAPublicKey();
            var privateKeyBytes = rsa.ExportRSAPrivateKey();

            var publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);
            var privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);

            migrationBuilder.InsertData(
                table: "rsa_key_pair",
                columns: new[] { "client_id", "public_key", "private_key" },
                values: new object[] { 1, publicKeyBase64, privateKeyBase64 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rsa_key_pair",
                keyColumn: "client_id",
                keyValue: 1);
        }
    }
}