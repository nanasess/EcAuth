using System;
using IdentityProvider.Models;
using IdentityProvider.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IdentityProvider.Test.Helpers
{
    public static class TestDbContextFactory
    {
        public static EcAuthDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<EcAuthDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetTenantId()).Returns("test-tenant");

            return new EcAuthDbContext(options, tenantServiceMock.Object);
        }
    }
}