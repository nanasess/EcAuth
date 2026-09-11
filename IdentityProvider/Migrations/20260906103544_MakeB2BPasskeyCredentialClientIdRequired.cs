using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class MakeB2BPasskeyCredentialClientIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 発行元カラムの NOT NULL 化（EcAuthDocs#110 リリース 4 / enforce）
            //
            // runbook（EcAuth CLAUDE.md「マイグレーション設計ルール」）に従い、制約強化は
            // 「違反データの解消」と「制約追加」を同一マイグレーションに置く。本番実測
            // （2026-09-06）では残存 NULL 0 件のため 1 本目は 0 件更新の保険だが、
            // migrate 実行時に旧インスタンスが行を作る可能性を潰すために残す。
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

            // 解消しきれない NULL が残っていたら、ここで明示的に失敗させる。
            //
            // EF が既定で生成する AlterColumn は defaultValue: 0 を伴い、残存 NULL を 0 で
            // 埋めたうえでカラム定義に DEFAULT 0 を残す。client_id = 0 に該当する Client は
            // 存在しないため FK 違反になるうえ、DEFAULT 制約もこのカラムには不要な残骸になる。
            // そのため defaultValue を外し、代わりに「何が残っているか」が分かる形で止める。
            migrationBuilder.Sql(@"
                EXEC('
                    IF EXISTS (SELECT 1 FROM dbo.b2b_passkey_credential WHERE client_id IS NULL)
                        THROW 50000, ''b2b_passkey_credential.client_id に NULL が残っています。b2b_user_identity から発行元を決定できない行があります（複数発行元 / identity 不在）。手動で補完してから再実行してください。'', 1;
                ');
            ");

            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "b2b_passkey_credential",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "b2b_passkey_credential",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
