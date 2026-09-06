using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class BackfillB2BPasskeyCredentialClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 追いつき backfill（EcAuthDocs#110 リリース 3 / transition）
            //
            // リリース 2 の backfill 完了から新コードのロールアウト完了までの窓では、旧インスタンス
            // （発行元を書かないコード）が作ったクレデンシャルが client_id = NULL のまま残る。
            // staging.yml / production.yml は migrate → build → deploy の順で走るため、この窓は
            // 構造的に避けられない。本リリースで allowCredentials を発行元で絞り始めるので、
            // 取りこぼした行をここで拾う（拾えなかった行は NULL 特例が候補に含めるため、
            // ログイン不能にはならない）。
            //
            // SQL はリリース 2 と同一。b2b_user_identity から発行元を引き、複数の発行元 identity を
            // 持つ b2b_subject は決定できないため NULL のまま残す（リリース 4 の NOT NULL 化までに
            // 解消する）。
            //
            // 冪等性: client_id IS NULL の行だけを対象にするため、再実行しても既存の値を壊さない。
            //
            // 注意: CLAUDE.md のルールに従い DML を EXEC() でラップする。client_id 列を追加するのは
            //       直前のマイグレーションだが、idempotent script では全マイグレーションが 1 バッチで
            //       コンパイルされるため、列が存在しない DB に対しては裸の SQL がコンパイルエラーになる。
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 補完した値を戻す手段はない（どの行が本マイグレーションで埋まったかを区別できない）。
            // ロールバックはリリース 2 のマイグレーションを戻して列ごと落とす形になる。
        }
    }
}
