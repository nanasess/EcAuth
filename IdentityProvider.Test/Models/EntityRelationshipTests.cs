using IdentityProvider.Models;
using IdentityProvider.Test.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Test.Models
{
    public class EntityRelationshipTests : IDisposable
    {
        private readonly EcAuthDbContext _context;

        public EntityRelationshipTests()
        {
            _context = TestDbContextHelper.CreateInMemoryContext();
        }

        [Fact]
        public async Task EcAuthUser_WithOrganization_ShouldSaveCorrectly()
        {
            var organization = new Organization
            {
                Code = "TEST",
                Name = "Test Organization",
                TenantName = "test-tenant"
            };
            
            var user = new EcAuthUser
            {
                Subject = "test-subject-123",
                EmailHash = "ABCD1234",
                OrganizationId = organization.Id,
                Organization = organization
            };

            _context.Organizations.Add(organization);
            _context.EcAuthUsers.Add(user);
            await _context.SaveChangesAsync();

            var savedUser = await _context.EcAuthUsers
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Subject == "test-subject-123");

            Assert.NotNull(savedUser);
            Assert.NotNull(savedUser.Organization);
            Assert.Equal("Test Organization", savedUser.Organization.Name);
        }

        [Fact]
        public async Task ExternalIdpMapping_WithEcAuthUser_ShouldSaveCorrectly()
        {
            var organization = new Organization
            {
                Code = "TEST",
                Name = "Test Organization",
                TenantName = "test-tenant"
            };

            var user = new EcAuthUser
            {
                Subject = "test-subject-123",
                EmailHash = "ABCD1234",
                OrganizationId = organization.Id,
                Organization = organization
            };

            var mapping = new ExternalIdpMapping
            {
                EcAuthSubject = user.Subject,
                ExternalProvider = "google",
                ExternalSubject = "google-123",
                EcAuthUser = user
            };

            _context.Organizations.Add(organization);
            _context.EcAuthUsers.Add(user);
            _context.ExternalIdpMappings.Add(mapping);
            await _context.SaveChangesAsync();

            var savedMapping = await _context.ExternalIdpMappings
                .Include(m => m.EcAuthUser)
                .FirstOrDefaultAsync(m => m.ExternalProvider == "google");

            Assert.NotNull(savedMapping);
            Assert.NotNull(savedMapping.EcAuthUser);
            Assert.Equal("test-subject-123", savedMapping.EcAuthUser.Subject);
        }

        [Fact]
        public async Task AuthorizationCode_WithEcAuthUserAndClient_ShouldSaveCorrectly()
        {
            var organization = new Organization
            {
                Code = "TEST",
                Name = "Test Organization",
                TenantName = "test-tenant"
            };

            var client = new Client
            {
                ClientId = "test-client",
                ClientSecret = "secret",
                AppName = "Test App",
                OrganizationId = organization.Id,
                Organization = organization
            };

            var user = new EcAuthUser
            {
                Subject = "test-subject-123",
                EmailHash = "ABCD1234",
                OrganizationId = organization.Id,
                Organization = organization
            };

            var authCode = new AuthorizationCode
            {
                Code = "auth-code-123",
                EcAuthSubject = user.Subject,
                ClientId = client.Id,
                RedirectUri = "https://example.com/callback",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                EcAuthUser = user,
                Client = client
            };

            _context.Organizations.Add(organization);
            _context.Clients.Add(client);
            _context.EcAuthUsers.Add(user);
            _context.AuthorizationCodes.Add(authCode);
            await _context.SaveChangesAsync();

            var savedAuthCode = await _context.AuthorizationCodes
                .Include(ac => ac.EcAuthUser)
                .Include(ac => ac.Client)
                .FirstOrDefaultAsync(ac => ac.Code == "auth-code-123");

            Assert.NotNull(savedAuthCode);
            Assert.NotNull(savedAuthCode.EcAuthUser);
            Assert.NotNull(savedAuthCode.Client);
            Assert.Equal("test-subject-123", savedAuthCode.EcAuthUser.Subject);
            Assert.Equal("test-client", savedAuthCode.Client.ClientId);
        }

        [Fact]
        public async Task EcAuthUser_WithMultipleExternalMappings_ShouldSaveCorrectly()
        {
            var organization = new Organization
            {
                Code = "TEST",
                Name = "Test Organization",
                TenantName = "test-tenant"
            };

            var user = new EcAuthUser
            {
                Subject = "test-subject-123",
                EmailHash = "ABCD1234",
                OrganizationId = organization.Id,
                Organization = organization
            };

            var googleMapping = new ExternalIdpMapping
            {
                EcAuthSubject = user.Subject,
                ExternalProvider = "google",
                ExternalSubject = "google-123",
                EcAuthUser = user
            };

            var lineMapping = new ExternalIdpMapping
            {
                EcAuthSubject = user.Subject,
                ExternalProvider = "line",
                ExternalSubject = "line-456",
                EcAuthUser = user
            };

            _context.Organizations.Add(organization);
            _context.EcAuthUsers.Add(user);
            _context.ExternalIdpMappings.AddRange(googleMapping, lineMapping);
            await _context.SaveChangesAsync();

            var savedUser = await _context.EcAuthUsers
                .Include(u => u.ExternalIdpMappings)
                .FirstOrDefaultAsync(u => u.Subject == "test-subject-123");

            Assert.NotNull(savedUser);
            Assert.Equal(2, savedUser.ExternalIdpMappings.Count);
            Assert.Contains(savedUser.ExternalIdpMappings, m => m.ExternalProvider == "google");
            Assert.Contains(savedUser.ExternalIdpMappings, m => m.ExternalProvider == "line");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}