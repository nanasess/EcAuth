using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddB2BPasskeyCredentialClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "client_id",
                table: "b2b_passkey_credential",
                type: "int",
                nullable: true);

            // 既存クレデンシャルの発行元を b2b_user_identity から補完する（EcAuthDocs#110 リリース 2）。
            //
            // 発行元は b2b_user_identity.client_id（EcAuth#521 で付与済み）から引く。「所属
            // Organization の唯一の B2B Client」から導出する方法もあるが、本 issue の目的が
            // 「1 Organization に複数 Client」を成立させることなので、その前提は将来崩れる。
            // identity は既に確定した発行元を持っているため、新しい前提を導入せずに補完できる。
            //
            // 1 つの b2b_subject が複数の発行元 identity を持つ場合は発行元を決定できないため
            // 補完しない（NULL のまま残す）。NULL はリリース 3 の絞り込みで「候補に含める」特例が
            // 拾い、リリース 4 の NOT NULL 化までに解消する。
            //
            // 注意: CLAUDE.md のルールに従い、本マイグレーションで追加した列を参照する DML は
            //       EXEC() でラップして名前解決を実行時まで遅延させる（idempotent script では全
            //       マイグレーションが 1 バッチでコンパイルされ、コンパイル時点では列が無いため）。
            //
            // 冪等性: client_id IS NULL の行だけを対象にするため、再実行しても既存の値を壊さない。
            migrationBuilder.Sql(@"
                EXEC('
                    UPDATE cred
                    SET cred.client_id = src.client_pk
                    FROM dbo.b2b_passkey_credential cred
                    INNER JOIN (
                        SELECT i.b2b_subject, MIN(c.id) AS client_pk
                        FROM dbo.b2b_user_identity i
                        INNER JOIN dbo.client c ON c.client_id = i.client_id
                        WHERE i.client_id IS NOT NULL
                        GROUP BY i.b2b_subject
                        HAVING COUNT(DISTINCT c.id) = 1
                    ) src ON src.b2b_subject = cred.b2b_subject
                    WHERE cred.client_id IS NULL
                ');
            ");

            migrationBuilder.CreateIndex(
                name: "IX_b2b_passkey_credential_client_id",
                table: "b2b_passkey_credential",
                column: "client_id");

            migrationBuilder.AddForeignKey(
                name: "FK_b2b_passkey_credential_client_client_id",
                table: "b2b_passkey_credential",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_b2b_passkey_credential_client_client_id",
                table: "b2b_passkey_credential");

            migrationBuilder.DropIndex(
                name: "IX_b2b_passkey_credential_client_id",
                table: "b2b_passkey_credential");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "b2b_passkey_credential");
        }
    }
}
