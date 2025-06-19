using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("ecauth_user")]
    public class EcAuthUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("subject")]
        [StringLength(255)]
        public string Subject { get; set; } = null!; // EcAuth内部識別子（UUID）

        [Column("email_hash")]
        [StringLength(255)]
        public string? EmailHash { get; set; } // ハッシュ化メールアドレス

        [Required]
        [Column("organization_id")]
        public int OrganizationId { get; set; } // テナント識別

        [ForeignKey(nameof(OrganizationId))]
        public Organization Organization { get; set; } = null!;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

        // ナビゲーションプロパティ
        public ICollection<ExternalIdpMapping> ExternalIdpMappings { get; set; } = new List<ExternalIdpMapping>();
    }
}