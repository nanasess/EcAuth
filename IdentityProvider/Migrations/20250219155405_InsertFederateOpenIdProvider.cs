using IdentityProvider.Models;
using IdentityProvider.Services;
using IdpUtilities.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertFederateOpenIdProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var MIGRATION_DB_HOST = DotNetEnv.Env.GetString("MIGRATION_DB_HOST");
            var DB_NAME = DotNetEnv.Env.GetString("DB_NAME");
            var DB_USER = DotNetEnv.Env.GetString("DB_USER");
            var DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");

            using (var scope = MigrationServiceProviderFactory<EcAuthDbContext>.CreateMigrationServiceProvider(
                    $"Server={MIGRATION_DB_HOST};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};TrustServerCertificate=true;MultipleActiveResultSets=true"
                )
                .AddScoped<ITenantService, TenantService>()
                .BuildServiceProvider()
                .CreateScope())
            {
                var CLIENT_ID = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
                var _context = scope.ServiceProvider.GetRequiredService<EcAuthDbContext>();
                var Client = _context.Clients.FirstOrDefault(c => c.ClientId == CLIENT_ID);
                _context.OpenIdProviders.Add(new OpenIdProvider
                {
                    Name = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_APP_NAME"),
                    IdpClientId = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_CLIENT_ID"),
                    IdpClientSecret = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_CLIENT_SECRET"),
                    AuthorizationEndpoint = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_AUTHORIZATION_ENDPOINT"),
                    TokenEndpoint = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_TOKEN_ENDPOINT"),
                    UserinfoEndpoint = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_USERINFO_ENDPOINT"),
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now,
                    Client = Client,
                    ClientId = Client.Id
                });
                _context.SaveChanges();
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            migrationBuilder.Sql($"DELETE FROM OpenIdProviders WHERE Name = '{DotNetEnv.Env.GetString("FEDERATE_OAUTH2_APP_NAME")}'");
        }
    }
}
