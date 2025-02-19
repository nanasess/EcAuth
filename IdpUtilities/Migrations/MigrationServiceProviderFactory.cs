using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdpUtilities.Migrations
{
    public class MigrationServiceProviderFactory<TContext> where TContext : DbContext
    {
        public static IServiceCollection CreateMigrationServiceProvider(string connectionString)
        {
            return new ServiceCollection()
                .AddDbContext<TContext>(options => options.UseSqlServer(connectionString));
        }
    }
}
