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
            migrationBuilder.Sql("INSERT INTO organization (code, name, created_at, updated_at) VALUES ('example', 'example', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)");
            migrationBuilder.Sql("UPDATE client SET organization_id = (SELECT id FROM organization WHERE code = 'example') WHERE client_id = 'client_id'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM organization WHERE code = 'example'");
            migrationBuilder.Sql("UPDATE client SET organization_id = NULL WHERE client_id = 'client_id'");
        }
    }
}
