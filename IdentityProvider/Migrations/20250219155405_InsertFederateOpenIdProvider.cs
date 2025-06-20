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
            // 環境変数から値を取得
            DotNetEnv.Env.TraversePath().Load();
            var FEDERATE_OAUTH2_APP_NAME = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_APP_NAME");
            var FEDERATE_OAUTH2_CLIENT_ID = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_CLIENT_ID");
            var FEDERATE_OAUTH2_CLIENT_SECRET = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_CLIENT_SECRET");
            var FEDERATE_OAUTH2_AUTHORIZATION_ENDPOINT = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_AUTHORIZATION_ENDPOINT");
            var FEDERATE_OAUTH2_TOKEN_ENDPOINT = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_TOKEN_ENDPOINT");
            var FEDERATE_OAUTH2_USERINFO_ENDPOINT = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_USERINFO_ENDPOINT");
            var DEFAULT_CLIENT_ID = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");

            // Federate OAuth2 プロバイダーの挿入（パラメータ化クエリ）
            migrationBuilder.Sql(@"
                INSERT INTO open_id_provider (
                    name, idp_client_id, idp_client_secret, 
                    authorization_endpoint, token_endpoint, userinfo_endpoint, 
                    created_at, updated_at, client_id
                )
                SELECT 
                    @p0,
                    @p1,
                    @p2,
                    @p3,
                    @p4,
                    @p5,
                    SYSDATETIMEOFFSET(),
                    SYSDATETIMEOFFSET(),
                    c.id
                FROM client c
                WHERE c.client_id = @p6",
                FEDERATE_OAUTH2_APP_NAME,
                FEDERATE_OAUTH2_CLIENT_ID,
                FEDERATE_OAUTH2_CLIENT_SECRET,
                FEDERATE_OAUTH2_AUTHORIZATION_ENDPOINT,
                FEDERATE_OAUTH2_TOKEN_ENDPOINT,
                FEDERATE_OAUTH2_USERINFO_ENDPOINT,
                DEFAULT_CLIENT_ID
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 環境変数から値を取得
            DotNetEnv.Env.TraversePath().Load();
            var FEDERATE_OAUTH2_APP_NAME = DotNetEnv.Env.GetString("FEDERATE_OAUTH2_APP_NAME");

            // パラメータ化クエリで削除
            migrationBuilder.Sql(@"
                DELETE FROM open_id_provider 
                WHERE name = @p0",
                FEDERATE_OAUTH2_APP_NAME
            );
        }
    }
}