using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddSurrogateKeyToEcAuthUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 外部キー制約を削除
            migrationBuilder.DropForeignKey(
                name: "FK_external_idp_mapping_ecauth_user_ecauth_subject",
                table: "external_idp_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_authorization_code_ecauth_user_ecauth_subject",
                table: "authorization_code");

            // 既存のプライマリキーを削除
            migrationBuilder.DropPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user");

            // 新しいID列を追加（サロゲートキー）
            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "ecauth_user",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // Subject列をAlternate Keyとして設定
            migrationBuilder.AddUniqueConstraint(
                name: "AK_ecauth_user_subject",
                table: "ecauth_user",
                column: "subject");

            // 新しいプライマリキーを追加
            migrationBuilder.AddPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user",
                column: "id");

            // Subjectのユニークインデックスを追加
            migrationBuilder.CreateIndex(
                name: "IX_ecauth_user_subject",
                table: "ecauth_user",
                column: "subject",
                unique: true);

            // 外部キー制約を再作成（AlternateKeyを参照）
            migrationBuilder.AddForeignKey(
                name: "FK_external_idp_mapping_ecauth_user_ecauth_subject",
                table: "external_idp_mapping",
                column: "ecauth_subject",
                principalTable: "ecauth_user",
                principalColumn: "subject",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_authorization_code_ecauth_user_ecauth_subject",
                table: "authorization_code",
                column: "ecauth_subject",
                principalTable: "ecauth_user",
                principalColumn: "subject",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 外部キー制約を削除
            migrationBuilder.DropForeignKey(
                name: "FK_external_idp_mapping_ecauth_user_ecauth_subject",
                table: "external_idp_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_authorization_code_ecauth_user_ecauth_subject",
                table: "authorization_code");

            // Alternate Keyを削除
            migrationBuilder.DropUniqueConstraint(
                name: "AK_ecauth_user_subject",
                table: "ecauth_user");

            // プライマリキーを削除
            migrationBuilder.DropPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user");

            // インデックスを削除
            migrationBuilder.DropIndex(
                name: "IX_ecauth_user_subject",
                table: "ecauth_user");

            // ID列を削除
            migrationBuilder.DropColumn(
                name: "id",
                table: "ecauth_user");

            // 元のプライマリキーを復元
            migrationBuilder.AddPrimaryKey(
                name: "PK_ecauth_user",
                table: "ecauth_user",
                column: "subject");

            // 外部キー制約を復元
            migrationBuilder.AddForeignKey(
                name: "FK_external_idp_mapping_ecauth_user_ecauth_subject",
                table: "external_idp_mapping",
                column: "ecauth_subject",
                principalTable: "ecauth_user",
                principalColumn: "subject",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_authorization_code_ecauth_user_ecauth_subject",
                table: "authorization_code",
                column: "ecauth_subject",
                principalTable: "ecauth_user",
                principalColumn: "subject",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
