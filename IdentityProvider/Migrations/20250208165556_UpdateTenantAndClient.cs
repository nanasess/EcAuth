using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantAndClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var DEFAULT_ORGANIZATION_TENANT_NAME = DotNetEnv.Env.GetString("DEFAULT_ORGANIZATION_TENANT_NAME");
            var DEFAULT_ORGANIZATION_CODE = DotNetEnv.Env.GetString("DEFAULT_ORGANIZATION_CODE");
            migrationBuilder.Sql($@"
                UPDATE organization
                SET tenant_name = '{DEFAULT_ORGANIZATION_TENANT_NAME}'
                WHERE code = '{DEFAULT_ORGANIZATION_CODE}'
            ");
            var DEFAULT_CLIENT_ID = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            var DEFAULT_APP_NAME = DotNetEnv.Env.GetString("DEFAULT_APP_NAME");
            migrationBuilder.Sql($@"
                UPDATE client
                SET app_name = '{DEFAULT_APP_NAME}'
                WHERE client_id = '{DEFAULT_CLIENT_ID}'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var DEFAULT_ORGANIZATION_CODE = DotNetEnv.Env.GetString("DEFAULT_ORGANIZATION_CODE");
            migrationBuilder.Sql($@"
                UPDATE organization
                SET tenant_name = NULL
                WHERE code = '{DEFAULT_ORGANIZATION_CODE}'
            ");
            var DEFAULT_CLIENT_ID = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            migrationBuilder.Sql($@"
                UPDATE client
                SET app_name = NULL
                WHERE client_id = '{DEFAULT_CLIENT_ID}'
            ");
        }
    }
}
