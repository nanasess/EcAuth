﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class InsertClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var client_id = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            var client_secret = DotNetEnv.Env.GetString("DEFAULT_CLIENT_SECRET");
            var app_name = DotNetEnv.Env.GetString("APP_NAME");
            migrationBuilder.Sql("SET IDENTITY_INSERT client ON");
            migrationBuilder.Sql(@$"
                INSERT INTO client (id, client_id, client_secret, app_name, created_at, updated_at)
                VALUES (1, '{client_id}', '{client_secret}', '{app_name}', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ");
            migrationBuilder.Sql("SET IDENTITY_INSERT client OFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DotNetEnv.Env.TraversePath().Load();
            var client_id = DotNetEnv.Env.GetString("DEFAULT_CLIENT_ID");
            migrationBuilder.Sql($"DELETE FROM client WHERE client_id = '{client_id}'");
        }
    }
}
