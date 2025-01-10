using System.ComponentModel;
using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider
{
    public class EcAuthDbContext : DbContext
    {
        public EcAuthDbContext()
        {
        }

        public DbSet<Client> Clients { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>;TrustServerCertificate=true;MultipleActiveResultSets=true");
        }
    }
}
