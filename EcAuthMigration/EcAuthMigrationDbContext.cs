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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=db;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder.RegisterOptionTypes(); // This method is not available in C#. You might need to implement it manually.
        }
    }
}
