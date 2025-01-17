using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityProvider.Models
{
    [Table("rsa_key_pair")]
    public class RsaKeyPair
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("public_key")]
        public string PublicKey { get; set; }
        [Column("private_key")]
        public string PrivateKey { get; set; }
        [Column("client_id")]
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;
    }
}
