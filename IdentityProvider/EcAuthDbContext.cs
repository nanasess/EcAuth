using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider
{
    public class EcAuthDbContext : DbContext
    {
        public EcAuthDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>;TrustServerCertificate=true;MultipleActiveResultSets=true");
        }
    }
}
