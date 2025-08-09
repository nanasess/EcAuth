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
        public DbSet<EcAuthUser> EcAuthUsers { get; set; }
        public DbSet<ExternalIdpMapping> ExternalIdpMappings { get; set; }
        public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 組織エンティティにテナントフィルターを適用
            modelBuilder.Entity<Organization>()
                .HasQueryFilter(o => o.TenantName == _tenantService.TenantName);

            // EcAuthUserにも同じグローバルクエリフィルターを適用
            modelBuilder.Entity<EcAuthUser>()
                .HasQueryFilter(u => u.Organization != null && u.Organization.TenantName == _tenantService.TenantName);

            // ExternalIdpMappingにもグローバルクエリフィルターを適用
            modelBuilder.Entity<ExternalIdpMapping>()
                .HasQueryFilter(m => m.EcAuthUser != null && m.EcAuthUser.Organization != null && m.EcAuthUser.Organization.TenantName == _tenantService.TenantName);

            // AuthorizationCodeにもグローバルクエリフィルターを適用
            modelBuilder.Entity<AuthorizationCode>()
                .HasQueryFilter(ac => ac.EcAuthUser != null && ac.EcAuthUser.Organization != null && ac.EcAuthUser.Organization.TenantName == _tenantService.TenantName);

            // EcAuthUser関連の設定
            modelBuilder.Entity<EcAuthUser>()
                .HasOne(u => u.Organization)
                .WithMany()
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EcAuthUser>()
                .HasMany(u => u.ExternalIdpMappings)
                .WithOne(m => m.EcAuthUser)
                .HasForeignKey(m => m.EcAuthSubject)
                .HasPrincipalKey(u => u.Subject)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EcAuthUser>()
                .HasMany(u => u.AuthorizationCodes)
                .WithOne(ac => ac.EcAuthUser)
                .HasForeignKey(ac => ac.EcAuthSubject)
                .HasPrincipalKey(u => u.Subject)
                .OnDelete(DeleteBehavior.Cascade);

            // AuthorizationCode関連の設定
            modelBuilder.Entity<AuthorizationCode>()
                .HasOne(ac => ac.Client)
                .WithMany()
                .HasForeignKey(ac => ac.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // インデックスの設定
            modelBuilder.Entity<EcAuthUser>()
                .HasIndex(u => new { u.OrganizationId, u.EmailHash })
                .IsUnique();

            modelBuilder.Entity<ExternalIdpMapping>()
                .HasIndex(m => new { m.ExternalProvider, m.ExternalSubject })
                .IsUnique();

            modelBuilder.Entity<AuthorizationCode>()
                .HasIndex(ac => ac.ExpiresAt);

            base.OnModelCreating(modelBuilder);
        }
    }
}
