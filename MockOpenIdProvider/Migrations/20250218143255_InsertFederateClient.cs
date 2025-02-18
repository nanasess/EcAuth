using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using MockOpenIdProvider.Migrations.Utilities;
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
            using (var scope = MigrationServiceProviderFactory.CreateMigrationServiceProvider().CreateScope())
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
