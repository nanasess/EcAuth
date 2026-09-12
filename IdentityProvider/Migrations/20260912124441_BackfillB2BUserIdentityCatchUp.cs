using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <summary>
    /// EcAuthDocs#110 リリース 5（transition）: b2b_user.external_id への参照をコードから外すのに合わせ、
    /// 追いつき backfill を行う。
    ///
    /// リリース 1（AddB2BUserIdentity）の backfill が終わってから二重書きコードのロールアウトが
    /// 完了するまでの窓では、旧インスタンスが作った b2b_user が identity 行を持たない。
    /// その行はコードが旧列を読まなくなると external_id で解決できなくなり、次のリリースで
    /// 列ごと消えて復旧元も失われる。本マイグレーションが走る時点で動いているのはリリース 1〜4 の
    /// 二重書きコードなので、ここで同じ SQL を再実行すれば取りこぼしが閉じる。
    ///
    /// 列そのものはまだ落とさない（contract は次のリリース）。B2BUser.ExternalId のマッピングは
    /// 本リリースのコードで既に外れているため scaffold は DropColumn を生成したが、意図的に除いている。
    /// 以降のスナップショットには external_id が無いので、次のリリースの DropIndex / DropColumn は
    /// scaffold されず手書きになる。
    /// </summary>
    public partial class BackfillB2BUserIdentityCatchUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OrganizationId の FK 索引。これまでは複合索引 IX_b2b_user_organization_id_external_id が
            // 先頭列で兼ねていたが、external_id をモデルから外したため EF の規約どおり単独索引を張る。
            // 複合索引は次のリリースで external_id 列と一緒に落とす。
            migrationBuilder.CreateIndex(
                name: "IX_b2b_user_organization_id",
                table: "b2b_user",
                column: "organization_id");

            // 追いつき backfill。AddB2BUserIdentity と同じ導出ルール・同じ NOT EXISTS ガードで冪等。
            // issuer_key は「その Organization が持つ唯一の Client」から導出する（HAVING COUNT(*) = 1）。
            // 唯一でない Organization のユーザーは決定的に補完できないため意図的に対象外とする
            //（以降は subject 一致でのみ解決される。identity 行は次回 register/options で作られる）。
            //
            // AddB2BUserIdentity との差分は ROW_NUMBER() による候補の重複排除。リリース 1 の migrate は
            // (organization_id, external_id) の UNIQUE を落としてから旧コードのロールアウトが完了するまで
            // 窓があり、旧コードは検索と INSERT を別々に行うため、同一 (organization_id, external_id) で
            // identity 無しの b2b_user が並行登録で 2 行できうる。NOT EXISTS は同一 INSERT ... SELECT 内の
            // 候補同士を比較しないので、そのまま流すと両方が同じ (issuer_key, external_id) を狙って
            // 一意違反（2601/2627）になり、本番の migrate ジョブが止まる。組ごとに最古の 1 行だけを
            // 候補にし、残りは subject 一致でのみ解決される状態のまま #63（孤児管理）に寄せる。
            //
            // CLAUDE.md のルールに従い、旧列 external_id の存在を sys.columns で確認し、DML は EXEC() で
            // ラップして名前解決を実行時まで遅延させる（列を落とす次のマイグレーション適用後の環境でも
            // 冪等スクリプトが通るようにするため）。
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE object_id = OBJECT_ID(N'dbo.b2b_user')
                           AND name = 'external_id')
                BEGIN
                    -- account_owner（accounts / stg-accounts）: 発行元は Account 型 Client
                    EXEC('
                        INSERT INTO dbo.b2b_user_identity (b2b_subject, issuer_key, external_id, client_id, created_at)
                        SELECT u.subject, ''client:'' + c.client_id, u.external_id, c.client_id, SYSDATETIMEOFFSET()
                        FROM (
                            SELECT subject, organization_id, external_id,
                                   ROW_NUMBER() OVER (PARTITION BY organization_id, external_id ORDER BY id) AS rn
                            FROM dbo.b2b_user
                            WHERE user_type = ''account_owner'' AND external_id <> ''''
                        ) u
                        INNER JOIN (
                            SELECT organization_id, MIN(client_id) AS client_id
                            FROM dbo.client
                            WHERE subject_type = 2 AND organization_id IS NOT NULL
                            GROUP BY organization_id
                            HAVING COUNT(*) = 1
                        ) c ON c.organization_id = u.organization_id
                        WHERE u.rn = 1
                          AND NOT EXISTS (
                              SELECT 1 FROM dbo.b2b_user_identity i
                              WHERE i.issuer_key = ''client:'' + c.client_id
                                AND i.external_id = u.external_id)
                    ');

                    -- それ以外（EC-CUBE プラグイン等の一般管理者）: 発行元は B2B 型 Client
                    EXEC('
                        INSERT INTO dbo.b2b_user_identity (b2b_subject, issuer_key, external_id, client_id, created_at)
                        SELECT u.subject, ''client:'' + c.client_id, u.external_id, c.client_id, SYSDATETIMEOFFSET()
                        FROM (
                            SELECT subject, organization_id, external_id,
                                   ROW_NUMBER() OVER (PARTITION BY organization_id, external_id ORDER BY id) AS rn
                            FROM dbo.b2b_user
                            WHERE user_type <> ''account_owner'' AND external_id <> ''''
                        ) u
                        INNER JOIN (
                            SELECT organization_id, MIN(client_id) AS client_id
                            FROM dbo.client
                            WHERE subject_type = 1 AND organization_id IS NOT NULL
                            GROUP BY organization_id
                            HAVING COUNT(*) = 1
                        ) c ON c.organization_id = u.organization_id
                        WHERE u.rn = 1
                          AND NOT EXISTS (
                              SELECT 1 FROM dbo.b2b_user_identity i
                              WHERE i.issuer_key = ''client:'' + c.client_id
                                AND i.external_id = u.external_id)
                    ');
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // backfill で追加した identity 行は正当なデータで、リリース 1 の backfill 由来の行と
            // 区別できないため戻さない。列は落としていないので AddColumn も不要。
            migrationBuilder.DropIndex(
                name: "IX_b2b_user_organization_id",
                table: "b2b_user");
        }
    }
}
