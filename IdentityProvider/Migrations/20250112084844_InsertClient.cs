using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO client (client_id, client_secret, app_name, created_at, updated_at) VALUES ('client_id', 'client_secret', 'app_name', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM client WHERE client_id = 'client_id'");
        }
    }
}
