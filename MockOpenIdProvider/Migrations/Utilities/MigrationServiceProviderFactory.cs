using Microsoft.EntityFrameworkCore;
using MockOpenIdProvider.Models;

namespace MockOpenIdProvider.Migrations.Utilities
{
    public static class MigrationServiceProviderFactory
    {
        public static IServiceProvider CreateMigrationServiceProvider()
        {
            DotNetEnv.Env.TraversePath().Load();
            var MIGRATION_DB_HOST = DotNetEnv.Env.GetString("MIGRATION_DB_HOST");
            var DB_NAME = DotNetEnv.Env.GetString("MOCK_IDP_DB_NAME");
            var DB_USER = DotNetEnv.Env.GetString("DB_USER");
            var DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");

            return new ServiceCollection()
                .AddDbContext<IdpDbContext>(options =>
                    options.UseSqlServer(
                        $"Server={MIGRATION_DB_HOST};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};" +
                        "TrustServerCertificate=true;MultipleActiveResultSets=true"))
                .BuildServiceProvider();
        }
    }

}
