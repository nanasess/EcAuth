using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertOpenIdProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 環境変数から値を取得
            DotNetEnv.Env.TraversePath().Load();
            var GOOGLE_OAUTH2_APP_NAME = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_APP_NAME");
            var GOOGLE_OAUTH2_CLIENT_ID = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_CLIENT_ID");
            var GOOGLE_OAUTH2_CLIENT_SECRET = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_CLIENT_SECRET");
            var GOOGLE_OAUTH2_DISCOVERY_URL = DotNetEnv.Env.GetString("GOOGLE_OAUTH2_DISCOVERY_URL");
            var AMAZON_OAUTH2_APP_NAME = DotNetEnv.Env.GetString("AMAZON_OAUTH2_APP_NAME");
            var AMAZON_OAUTH2_CLIENT_ID = DotNetEnv.Env.GetString("AMAZON_OAUTH2_CLIENT_ID");
            var AMAZON_OAUTH2_CLIENT_SECRET = DotNetEnv.Env.GetString("AMAZON_OAUTH2_CLIENT_SECRET");
            var AMAZON_OAUTH2_AUTHORIZATION_ENDPOINT = DotNetEnv.Env.GetString("AMAZON_OAUTH2_AUTHORIZATION_ENDPOINT");
            var AMAZON_OAUTH2_TOKEN_ENDPOINT = DotNetEnv.Env.GetString("AMAZON_OAUTH2_TOKEN_ENDPOINT");
            var AMAZON_OAUTH2_USERINFO_ENDPOINT = DotNetEnv.Env.GetString("AMAZON_OAUTH2_USERINFO_ENDPOINT");
            var DEFAULT_CLIENT_ID = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");

            // Google OAuth2 プロバイダーの挿入
            migrationBuilder.Sql($@"
                INSERT INTO open_id_provider (
                    name, idp_client_id, idp_client_secret, discovery_document_uri, 
                    issuer, authorization_endpoint, token_endpoint, userinfo_endpoint, 
                    jwks_uri, created_at, updated_at, client_id
                )
                SELECT 
                    '{GOOGLE_OAUTH2_APP_NAME}',
                    '{GOOGLE_OAUTH2_CLIENT_ID}',
                    '{GOOGLE_OAUTH2_CLIENT_SECRET}',
                    '{GOOGLE_OAUTH2_DISCOVERY_URL}',
                    'https://accounts.google.com',
                    'https://accounts.google.com/o/oauth2/v2/auth',
                    'https://oauth2.googleapis.com/token',
                    'https://openidconnect.googleapis.com/v1/userinfo',
                    'https://www.googleapis.com/oauth2/v3/certs',
                    SYSDATETIMEOFFSET(),
                    SYSDATETIMEOFFSET(),
                    c.id
                FROM client c
                WHERE c.client_id = '{DEFAULT_CLIENT_ID}'
            ");

            // Amazon OAuth2 プロバイダーの挿入
            migrationBuilder.Sql($@"
                INSERT INTO open_id_provider (
                    name, idp_client_id, idp_client_secret, 
                    authorization_endpoint, token_endpoint, userinfo_endpoint, 
                    created_at, updated_at, client_id
                )
                SELECT 
                    '{AMAZON_OAUTH2_APP_NAME}',
                    '{AMAZON_OAUTH2_CLIENT_ID}',
                    '{AMAZON_OAUTH2_CLIENT_SECRET}',
                    '{AMAZON_OAUTH2_AUTHORIZATION_ENDPOINT}',
                    '{AMAZON_OAUTH2_TOKEN_ENDPOINT}',
                    '{AMAZON_OAUTH2_USERINFO_ENDPOINT}',
                    SYSDATETIMEOFFSET(),
                    SYSDATETIMEOFFSET(),
                    c.id
                FROM client c
                WHERE c.client_id = '{DEFAULT_CLIENT_ID}'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM open_id_provider");
        }
    }
}