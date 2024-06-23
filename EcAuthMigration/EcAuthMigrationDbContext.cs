using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using static Models;

namespace EcAuthMigration
{
    public class EcAuthMigrationDbContext : DbContext
    {
        public EcAuthMigrationDbContext()
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<RsaKeyPair> RsaKeyPairs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>;TrustServerCertificate=true;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>();
            modelBuilder.Entity<RsaKeyPair>(b => {
                b.HasOne<Client>().WithMany().HasForeignKey("ClientId");
            });
        }
    }
}
