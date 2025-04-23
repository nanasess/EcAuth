using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockOpenIdProvider.Models
{
    [Table("refresh_token")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("token")]
        public string Token { get; set; }
        [Column("expires_in")]
        public int ExpiresIn { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("client_id")]
        public int ClientId { get; set; }
        public Client Client { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        public MockIdpUser User { get; set; }
    }
}
