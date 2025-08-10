using IdentityProvider.Models;
using IdentityProvider.Services;
using IdentityProvider.Test.TestHelpers;
using IdpUtilities;

namespace IdentityProvider.Test.Services
{
    public class UserServiceTests
    {
        [Fact]
        public async Task GetOrCreateUserAsync_NewUser_ShouldCreateUserAndMapping()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Arrange
            var organization = new Organization { Id = 1, Code = "TESTORG", Name = "TestOrg", TenantName = "test-tenant" };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync();

            var request = new IUserService.UserCreationRequest
            {
                ExternalProvider = "google",
                ExternalSubject = "google-user-123",
                EmailHash = EmailHashUtil.HashEmail("test@example.com"),
                OrganizationId = 1
            };

            // Act
            var result = await service.GetOrCreateUserAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Subject);
            Assert.Equal(request.EmailHash, result.EmailHash);
            Assert.Equal(request.OrganizationId, result.OrganizationId);

            // Verify user was created in database
            var userInDb = await context.EcAuthUsers.FindAsync(result.Subject);
            Assert.NotNull(userInDb);

            // Verify external IdP mapping was created
            var mappingInDb = context.ExternalIdpMappings
                .FirstOrDefault(m => m.EcAuthSubject == result.Subject);
            Assert.NotNull(mappingInDb);
            Assert.Equal(request.ExternalProvider, mappingInDb.ExternalProvider);
            Assert.Equal(request.ExternalSubject, mappingInDb.ExternalSubject);
        }

        [Fact]
        public async Task GetOrCreateUserAsync_ExistingUser_ShouldReturnExistingUser()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Arrange
            var organization = new Organization { Id = 1, Code = "TESTORG", Name = "TestOrg", TenantName = "test-tenant" };
            context.Organizations.Add(organization);

            var existingUser = new EcAuthUser
            {
                Subject = Guid.NewGuid().ToString(),
                EmailHash = EmailHashUtil.HashEmail("test@example.com"),
                OrganizationId = 1
            };
            context.EcAuthUsers.Add(existingUser);

            var existingMapping = new ExternalIdpMapping
            {
                EcAuthSubject = existingUser.Subject,
                ExternalProvider = "google",
                ExternalSubject = "google-user-123"
            };
            context.ExternalIdpMappings.Add(existingMapping);
            await context.SaveChangesAsync();

            var request = new IUserService.UserCreationRequest
            {
                ExternalProvider = "google",
                ExternalSubject = "google-user-123",
                EmailHash = EmailHashUtil.HashEmail("updated@example.com"),
                OrganizationId = 1
            };

            // Act
            var result = await service.GetOrCreateUserAsync(request);

            // Assert
            Assert.Equal(existingUser.Subject, result.Subject);
            Assert.Equal(request.EmailHash, result.EmailHash); // Should be updated
            Assert.Equal(existingUser.OrganizationId, result.OrganizationId);

            // Verify email hash was updated
            var userInDb = await context.EcAuthUsers.FindAsync(existingUser.Subject);
            Assert.NotNull(userInDb);
            Assert.Equal(request.EmailHash, userInDb.EmailHash);
        }

        [Theory]
        [InlineData(null, "external-subject", "email-hash", 1)]
        [InlineData("", "external-subject", "email-hash", 1)]
        [InlineData("google", null, "email-hash", 1)]
        [InlineData("google", "", "email-hash", 1)]
        [InlineData("google", "external-subject", null, 1)]
        [InlineData("google", "external-subject", "", 1)]
        [InlineData("google", "external-subject", "email-hash", 0)]
        [InlineData("google", "external-subject", "email-hash", -1)]
        public async Task GetOrCreateUserAsync_InvalidRequest_ShouldThrowArgumentException(
            string? externalProvider, string? externalSubject, string? emailHash, int organizationId)
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            var request = new IUserService.UserCreationRequest
            {
                ExternalProvider = externalProvider ?? "",
                ExternalSubject = externalSubject ?? "",
                EmailHash = emailHash ?? "",
                OrganizationId = organizationId
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetOrCreateUserAsync(request));
        }

        [Fact]
        public async Task GetUserBySubjectAsync_ExistingUser_ShouldReturnUser()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Arrange
            var organization = new Organization { Id = 1, Code = "TESTORG", Name = "TestOrg", TenantName = "test-tenant" };
            context.Organizations.Add(organization);

            var user = new EcAuthUser
            {
                Subject = "test-subject",
                EmailHash = EmailHashUtil.HashEmail("test@example.com"),
                OrganizationId = 1
            };
            context.EcAuthUsers.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetUserBySubjectAsync("test-subject");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Subject, result.Subject);
            Assert.Equal(user.EmailHash, result.EmailHash);
            Assert.Equal(user.OrganizationId, result.OrganizationId);
        }

        [Fact]
        public async Task GetUserBySubjectAsync_NonExistingUser_ShouldReturnNull()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Act
            var result = await service.GetUserBySubjectAsync("non-existing-subject");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetUserBySubjectAsync_InvalidSubject_ShouldReturnNull(string? subject)
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Act
            var result = await service.GetUserBySubjectAsync(subject);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByExternalIdAsync_ExistingMapping_ShouldReturnUser()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Arrange
            var organization = new Organization { Id = 1, Code = "TESTORG", Name = "TestOrg", TenantName = "test-tenant" };
            context.Organizations.Add(organization);

            var user = new EcAuthUser
            {
                Subject = "test-subject",
                EmailHash = EmailHashUtil.HashEmail("test@example.com"),
                OrganizationId = 1
            };
            context.EcAuthUsers.Add(user);

            var mapping = new ExternalIdpMapping
            {
                EcAuthSubject = user.Subject,
                ExternalProvider = "google",
                ExternalSubject = "google-user-123"
            };
            context.ExternalIdpMappings.Add(mapping);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetUserByExternalIdAsync("google", "google-user-123", 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Subject, result.Subject);
            Assert.Equal(user.EmailHash, result.EmailHash);
            Assert.Equal(user.OrganizationId, result.OrganizationId);
        }

        [Fact]
        public async Task GetUserByExternalIdAsync_DifferentOrganization_ShouldReturnNull()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Arrange
            var organization = new Organization { Id = 1, Code = "TESTORG", Name = "TestOrg", TenantName = "test-tenant" };
            context.Organizations.Add(organization);

            var user = new EcAuthUser
            {
                Subject = "test-subject",
                EmailHash = EmailHashUtil.HashEmail("test@example.com"),
                OrganizationId = 1
            };
            context.EcAuthUsers.Add(user);

            var mapping = new ExternalIdpMapping
            {
                EcAuthSubject = user.Subject,
                ExternalProvider = "google",
                ExternalSubject = "google-user-123"
            };
            context.ExternalIdpMappings.Add(mapping);
            await context.SaveChangesAsync();

            // Act - Search for different organization
            var result = await service.GetUserByExternalIdAsync("google", "google-user-123", 2);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByExternalIdAsync_NonExistingMapping_ShouldReturnNull()
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Act
            var result = await service.GetUserByExternalIdAsync("google", "non-existing-subject", 1);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null, "external-subject")]
        [InlineData("", "external-subject")]
        [InlineData("google", null)]
        [InlineData("google", "")]
        public async Task GetUserByExternalIdAsync_InvalidParameters_ShouldReturnNull(
            string? externalProvider, string? externalSubject)
        {
            using var context = TestDbContextHelper.CreateInMemoryContext();
            var service = new UserService(context);

            // Act
            var result = await service.GetUserByExternalIdAsync(externalProvider, externalSubject, 1);

            // Assert
            Assert.Null(result);
        }
    }
}