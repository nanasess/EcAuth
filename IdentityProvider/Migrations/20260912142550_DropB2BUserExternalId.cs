using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <summary>
    /// EcAuthDocs#110 リリース 6（contract）: b2b_user.external_id 列と複合索引を削除する。
    ///
    /// 識別子の置き場は b2b_user_identity（issuer_key, external_id）に一本化済み。旧列への参照は
    /// リリース 5（BackfillB2BUserIdentityCatchUp / EcAuth#538）でコードから完全に外れ、EF マッピングも
    /// 無いので、本マイグレーションの migrate 時に動いているコードは列の有無に依存しない。
    ///
    /// EF マッピングを外した時点でスナップショットから列が消えているため、dotnet ef migrations add は
    /// 空の scaffold を生成する。DropIndex / DropColumn は手書き。
    ///
    /// 削除される情報は b2b_user.external_id のハッシュ値のみ。identity 無しの b2b_user はリリース 5 の
    /// 追いつき backfill とガード（identity 無し + パスキー登録済みなら THROW）で本番 0 件を確認済みなので、
    /// 識別に必要な値はすべて b2b_user_identity 側にある。
    /// </summary>
    public partial class DropB2BUserExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 列を含む複合索引を先に落とす（SQL Server は索引が依存する列を落とせない: Msg 5074）。
            // organization_id の FK 索引はリリース 5 で IX_b2b_user_organization_id を単独で張ってあるので、
            // 複合索引を落としても FK 側の索引は残る。
            migrationBuilder.DropIndex(
                name: "IX_b2b_user_organization_id_external_id",
                table: "b2b_user");

            // MakeB2BUserExternalIdRequired が付けた DEFAULT 制約（DF__b2b_user__extern__…）は、
            // EF の SQL Server 生成器が sys.default_constraints から名前を引いて先に落としてから
            // DROP COLUMN する（生成される冪等スクリプトで確認済み）。
            migrationBuilder.DropColumn(
                name: "external_id",
                table: "b2b_user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 列と索引の形だけ戻す（AddB2BUserIdentity 以降の非一意）。値は復元されず全行 '' になるが、
            // リリース 5 以降のコードは列を読まないので動作には影響しない。値の復旧が要る場合は PITR。
            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "b2b_user",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_b2b_user_organization_id_external_id",
                table: "b2b_user",
                columns: new[] { "organization_id", "external_id" });
        }
    }
}
