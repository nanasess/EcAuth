using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("b2b_user")]
    public class B2BUser : ISubjectProvider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("subject")]
        [MaxLength(255)]
        [Required]
        public string Subject { get; set; } = string.Empty;

        [Column("user_type")]
        [MaxLength(50)]
        [Required]
        public string UserType { get; set; } = "admin";

        [Column("organization_id")]
        [Required]
        public int OrganizationId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Organization? Organization { get; set; }
        public ICollection<B2BPasskeyCredential> PasskeyCredentials { get; } = new List<B2BPasskeyCredential>();

        /// <summary>
        /// 発行元ごとの識別子。1 人が EC-CUBE / WordPress / 企業SSO など複数の発行元から
        /// 到達しうるため 1:N で持つ（EcAuthDocs#110）。
        ///
        /// 識別子（external_id）の置き場はここだけ。旧 b2b_user.external_id 列は DB にはまだ
        /// 存在するが（DEFAULT '' で INSERT 時に省略できる）、本エンティティにはマップしない。
        /// EF Core はマップ済みプロパティを全クエリの SELECT に含めるため、マップを残したまま
        /// 列を落とすと b2b_user の全読み取りが失敗する。列の削除は次のリリースで行う。
        /// </summary>
        public ICollection<B2BUserIdentity> Identities { get; } = new List<B2BUserIdentity>();
    }
}
