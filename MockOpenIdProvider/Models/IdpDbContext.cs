using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace MockOpenIdProvider.Models
{
    public class IdpDbContext : DbContext
    {
        public IdpDbContext(DbContextOptions<IdpDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
        public DbSet<AccessToken> AccessTokens { get; set; }
        public DbSet<MockIdpUser> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
