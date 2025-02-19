using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using IdpUtilities.Migrations;
using MockOpenIdProvider.Models;

#nullable disable

namespace MockOpenIdProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertMockIdpUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var MIGRATION_DB_HOST = DotNetEnv.Env.GetString("MIGRATION_DB_HOST");
            var DB_NAME = DotNetEnv.Env.GetString("MOCK_IDP_DB_NAME");
            var DB_USER = DotNetEnv.Env.GetString("DB_USER");
            var DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");
            var clientId = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            using (var scope = MigrationServiceProviderFactory<IdpDbContext>.CreateMigrationServiceProvider(
                $"Server={MIGRATION_DB_HOST};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};TrustServerCertificate=true;MultipleActiveResultSets=true"
                ).CreateScope())
            {
                var CLIENT_ID = DotNetEnv.Env.GetString("MOCK_IDP_DEFAULT_CLIENT_ID");
                var _context = scope.ServiceProvider.GetRequiredService<IdpDbContext>();
                var Client = _context.Clients.FirstOrDefault(c => c.ClientId == CLIENT_ID);
                PasswordHasher<MockIdpUser> passwordHasher = new PasswordHasher<MockIdpUser>();
                var user = new MockIdpUser
                {
                    Email = DotNetEnv.Env.GetString("MOCK_IDP_DEFAULT_USER_EMAIL"),
                    Password = string.Empty,
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now,
                    ClientId = Client.Id,
                    Client = Client
                };
                var password = DotNetEnv.Env.GetString("MOCK_IDP_DEFAULT_USER_PASSWORD");
                user.Password = passwordHasher.HashPassword(user, password);
                Console.WriteLine($"Hashed password: {user.Password}");
                _context.Users.Add(user);
                _context.SaveChanges();

                var validation = passwordHasher.VerifyHashedPassword(user, user.Password, password);
                if (validation == PasswordVerificationResult.Failed)
                {
                    throw new Exception("Password validation failed");
                }
                else
                {
                    Console.WriteLine($"Password validation: {validation}");;
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM mock_idp_user");
        }
    }
}
