using System.ComponentModel;
using IdentityProvider.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Models
{
    public class EcAuthDbContext : DbContext
    {
        private readonly ITenantService _tenantService;

        public EcAuthDbContext(DbContextOptions<EcAuthDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<RsaKeyPair> RsaKeyPairs { get; set; }
        public DbSet<RedirectUri> RedirectUris { get; set; }
        public DbSet<OpenIdProvider> OpenIdProviders { get; set; }
        public DbSet<OpenIdProviderScope> OpenIdProviderScopes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 組織エンティティにテナントフィルターを適用
            modelBuilder.Entity<Organization>()
                .HasQueryFilter(o => o.TenantName == _tenantService.TenantName);

            base.OnModelCreating(modelBuilder);
        }
    }
}
