using System.ComponentModel;
using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider
{
    public class EcAuthDbContext : DbContext
    {
        public EcAuthDbContext(DbContextOptions<EcAuthDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<RsaKeyPair> RsaKeyPairs { get; set; }
        public DbSet<RedirectUri> RedirectUris { get; set; }
        public DbSet<OpenIdProvider> OpenIdProviders { get; set; }
        public DbSet<OpenIdProviderScope> OpenIdProviderScopes { get; set; }
        // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // {
        //     optionsBuilder.UseSqlServer("Server=db;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>;TrustServerCertificate=true;MultipleActiveResultSets=true");
        // }
    }
}
