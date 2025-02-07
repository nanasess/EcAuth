using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var client_id = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            var organization_code = DotNetEnv.Env.GetString("DEFAULT_ORGANIZATION_CODE");
            migrationBuilder.Sql($"UPDATE client SET organization_id = (SELECT TOP 1 id FROM organization WHERE code = '{organization_code}') WHERE client_id = '{client_id}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var client_id = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            migrationBuilder.Sql($"UPDATE client SET organization_id = NULL WHERE client_id = '{client_id}'");
        }
    }
}
