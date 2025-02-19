using System.Security.Cryptography;
using IdpUtilities.Migrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using MockOpenIdProvider.Models;

#nullable disable

namespace MockOpenIdProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertFederateClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var MIGRATION_DB_HOST = DotNetEnv.Env.GetString("MIGRATION_DB_HOST");
            var DB_NAME = DotNetEnv.Env.GetString("MOCK_IDP_DB_NAME");
            var DB_USER = DotNetEnv.Env.GetString("DB_USER");
            var DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");
            using (var scope = MigrationServiceProviderFactory<IdpDbContext>.CreateMigrationServiceProvider(
                $"Server={MIGRATION_DB_HOST};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};TrustServerCertificate=true;MultipleActiveResultSets=true"
                ).CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<IdpDbContext>();
                var clientId = DotNetEnv.Env.GetString("MOCK_IDP_FEDERATE_CLIENT_ID");
                var clientSecret = DotNetEnv.Env.GetString("MOCK_IDP_FEDERATE_CLIENT_SECRET");
                var clientName = DotNetEnv.Env.GetString("MOCK_IDP_FEDERATE_CLIENT_NAME");
                var redirectUri = DotNetEnv.Env.GetString("DEFAULT_ORGANIZATION_REDIRECT_URI");
                using RSA rsa = RSA.Create();
                var privateKey = rsa.ExportRSAPrivateKeyPem();
                var publicKey = rsa.ExportRSAPublicKeyPem();

                var Client = new Client
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    ClientName = clientName,
                    RedirectUri = redirectUri,
                    PublicKey = publicKey,
                    PrivateKey = privateKey
                };
                _context.Clients.Add(Client);
                _context.SaveChanges();

                PasswordHasher<MockIdpUser> passwordHasher = new PasswordHasher<MockIdpUser>();
                var user = new MockIdpUser
                {
                    Email = DotNetEnv.Env.GetString("MOCK_IDP_FEDERATE_USER_EMAIL"),
                    Password = string.Empty,
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now,
                    ClientId = Client.Id,
                    Client = Client
                };
                var password = DotNetEnv.Env.GetString("MOCK_IDP_DEFAULT_USER_PASSWORD");
                user.Password = passwordHasher.HashPassword(user, password);
                _context.Users.Add(user);
                _context.SaveChanges();
            }


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            migrationBuilder.Sql($"DELETE FROM mock_idp_user WHERE email = '{DotNetEnv.Env.GetString("MOCK_IDP_FEDERATE_USER_EMAIL")}'");
            migrationBuilder.Sql($"DELETE FROM client WHERE client_id = '{DotNetEnv.Env.GetString("MOCK_IDP_FEDERATE_CLIENT_ID")}'");

        }
    }
}
