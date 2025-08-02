using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("authorization_code")]
    public class AuthorizationCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("code")]
        [MaxLength(255)]
        public string Code { get; set; } = string.Empty;

        [Column("ecauth_subject")]
        [MaxLength(255)]
        [Required]
        public string EcAuthSubject { get; set; } = string.Empty;

        [Column("client_id")]
        [MaxLength(255)]
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Column("redirect_uri")]
        [MaxLength(2000)]
        [Required]
        public string RedirectUri { get; set; } = string.Empty;

        [Column("scope")]
        [MaxLength(500)]
        public string? Scope { get; set; }

        [Column("state")]
        [MaxLength(500)]
        public string? State { get; set; }

        [Column("expires_at")]
        public DateTimeOffset ExpiresAt { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; } = false;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("used_at")]
        public DateTimeOffset? UsedAt { get; set; }

        public EcAuthUser? EcAuthUser { get; set; }
        public Client? Client { get; set; }
    }
}