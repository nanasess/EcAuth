using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("authorization_code")]
    public class AuthorizationCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("code")]
        [StringLength(255)]
        public string Code { get; set; } = null!;

        [Required]
        [Column("client_id")]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;

        [Required]
        [Column("ecauth_user_id")]
        public int EcAuthUserId { get; set; }

        [ForeignKey(nameof(EcAuthUserId))]
        public EcAuthUser EcAuthUser { get; set; } = null!;

        [Column("redirect_uri")]
        [StringLength(500)]
        public string? RedirectUri { get; set; }

        [Column("scope")]
        [StringLength(500)]
        public string? Scope { get; set; }

        [Column("expires_at")]
        public DateTimeOffset ExpiresAt { get; set; }

        [Column("used")]
        public bool Used { get; set; } = false;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}