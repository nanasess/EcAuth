using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("client")]
    public class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("client_id")]
        public string ClientId { get; set; }
        [Column("client_secret")]
        public string ClientSecret { get; set; }
        [Column("app_name")]
        public string AppName { get; set; }
        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        [Column("organization_id")]
        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public RsaKeyPair? RsaKeyPair { get; set; }
        public ICollection<RedirectUri>? RedirectUris { get; } = new List<RedirectUri>();
        public ICollection<OpenIdProvider>? OpenIdProviders { get; } = new List<OpenIdProvider>();
    }
}
