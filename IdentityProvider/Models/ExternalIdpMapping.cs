using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("external_idp_mapping")]
    public class ExternalIdpMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("ecauth_user_id")]
        public int EcAuthUserId { get; set; }

        [ForeignKey(nameof(EcAuthUserId))]
        public EcAuthUser EcAuthUser { get; set; } = null!;

        [Required]
        [Column("external_provider")]
        [StringLength(50)]
        public string ExternalProvider { get; set; } = null!; // "google", "line", etc.

        [Required]
        [Column("external_subject")]
        [StringLength(255)]
        public string ExternalSubject { get; set; } = null!; // 外部IdPのsubject

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}