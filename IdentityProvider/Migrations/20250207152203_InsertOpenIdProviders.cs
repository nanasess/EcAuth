using IdentityProvider.Models;
using IdentityProvider.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertOpenIdProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var MIGRATION_DB_HOST = DotNetEnv.Env.GetString("MIGRATION_DB_HOST");
            var DB_NAME = DotNetEnv.Env.GetString("DB_NAME");
            var DB_USER = DotNetEnv.Env.GetString("DB_USER");
            var DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");
            var GoogleOauth2AppName = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_APP_NAME");
            Console.WriteLine($"GoogleOauth2AppName: {GoogleOauth2AppName}");

            var serviceProvider = new ServiceCollection()
                .AddScoped<ITenantService, TenantService>()
                .AddDbContext<EcAuthDbContext>(options =>
                    options.UseSqlServer($"Server={MIGRATION_DB_HOST};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};TrustServerCertificate=true;MultipleActiveResultSets=true"))
                .BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<EcAuthDbContext>();
                _context.OpenIdProviders.Add(new OpenIdProvider
                {
                    Name = "Google",
                    IdpClientId = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_CLIENT_ID"),
                    IdpClientSecret = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_CLIENT_SECRET"),
                    DiscoveryDocumentUri = "https://accounts.google.com/.well-known/openid-configuration",
                    Issuer = "https://accounts.google.com",
                    AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
                    TokenEndpoint = "https://oauth2.googleapis.com/token",
                    UserinfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo",
                    JwksUri = "https://www.googleapis.com/oauth2/v3/certs",
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now
                });
                _context.SaveChanges();
            }


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
