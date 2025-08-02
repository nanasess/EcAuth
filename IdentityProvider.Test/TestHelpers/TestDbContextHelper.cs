using IdentityProvider.Models;
using IdentityProvider.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Test.TestHelpers
{
    public static class TestDbContextHelper
    {
        public static EcAuthDbContext CreateInMemoryContext(string? databaseName = null)
        {
            var dbName = databaseName ?? Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<EcAuthDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var mockTenantService = new MockTenantService();
            return new EcAuthDbContext(options, mockTenantService);
        }
    }

    public class MockTenantService : ITenantService
    {
        public string TenantName { get; private set; } = "test-tenant";

        public void SetTenant(string tenantName)
        {
            TenantName = tenantName;
        }
    }
}