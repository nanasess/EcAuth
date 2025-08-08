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

        [Column("ecauth_subject")]
        [MaxLength(255)]
        [Required]
        public string EcAuthSubject { get; set; } = string.Empty;

        [Column("external_provider")]
        [MaxLength(100)]
        [Required]
        public string ExternalProvider { get; set; } = string.Empty;

        [Column("external_subject")]
        [MaxLength(255)]
        [Required]
        public string ExternalSubject { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public EcAuthUser? EcAuthUser { get; set; }
    }
}