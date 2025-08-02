using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityProvider.Models;
using IdentityProvider.Services;
using IdentityProvider.Test.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityProvider.Test.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly EcAuthDbContext _context;
        private readonly UserService _userService;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Organization _testOrganization;

        public UserServiceTests()
        {
            _context = TestDbContextFactory.CreateTestDbContext();
            _loggerMock = new Mock<ILogger<UserService>>();
            _userService = new UserService(_context, _loggerMock.Object);

            // Setup test organization
            _testOrganization = new Organization
            {
                Code = "test-org",
                Name = "Test Organization",
                TenantName = "test-tenant"
            };
            _context.Organizations.Add(_testOrganization);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetOrCreateUserAsync_CreatesNewUser_WhenUserDoesNotExist()
        {
            // Arrange
            var request = new UserCreationRequest
            {
                ExternalProvider = "test-provider",
                ExternalSubject = "external-subject-123",
                Email = "test@example.com",
                OrganizationId = _testOrganization.Id
            };

            // Act
            var user = await _userService.GetOrCreateUserAsync(request);

            // Assert
            Assert.NotNull(user);
            Assert.NotEmpty(user.Subject);
            Assert.Equal(_userService.HashEmail(request.Email), user.EmailHash);
            Assert.Equal(request.OrganizationId, user.OrganizationId);

            // Verify user was saved to database
            var savedUser = await _context.EcAuthUsers
                .Include(u => u.ExternalIdpMappings)
                .FirstOrDefaultAsync(u => u.Subject == user.Subject);
            Assert.NotNull(savedUser);
            Assert.Single(savedUser.ExternalIdpMappings);
            Assert.Equal(request.ExternalProvider, savedUser.ExternalIdpMappings.First().ExternalProvider);
            Assert.Equal(request.ExternalSubject, savedUser.ExternalIdpMappings.First().ExternalSubject);
        }

        [Fact]
        public async Task GetOrCreateUserAsync_ReturnsExistingUser_WhenUserExists()
        {
            // Arrange
            var request = new UserCreationRequest
            {
                ExternalProvider = "test-provider",
                ExternalSubject = "external-subject-123",
                Email = "test@example.com",
                OrganizationId = _testOrganization.Id
            };

            // Create user first
            var firstUser = await _userService.GetOrCreateUserAsync(request);

            // Act - Try to create same user again
            var secondUser = await _userService.GetOrCreateUserAsync(request);

            // Assert
            Assert.Equal(firstUser.Subject, secondUser.Subject);
            Assert.Equal(firstUser.EmailHash, secondUser.EmailHash);

            // Verify only one user exists
            var userCount = await _context.EcAuthUsers.CountAsync();
            Assert.Equal(1, userCount);
        }

        [Fact]
        public async Task GetUserBySubjectAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var request = new UserCreationRequest
            {
                ExternalProvider = "test-provider",
                ExternalSubject = "external-subject-123",
                Email = "test@example.com",
                OrganizationId = _testOrganization.Id
            };

            var createdUser = await _userService.GetOrCreateUserAsync(request);

            // Act
            var retrievedUser = await _userService.GetUserBySubjectAsync(createdUser.Subject);

            // Assert
            Assert.NotNull(retrievedUser);
            Assert.Equal(createdUser.Subject, retrievedUser.Subject);
            Assert.Equal(createdUser.EmailHash, retrievedUser.EmailHash);
            Assert.Equal(createdUser.OrganizationId, retrievedUser.OrganizationId);
        }

        [Fact]
        public async Task GetUserBySubjectAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Act
            var user = await _userService.GetUserBySubjectAsync("non-existent-subject");

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByExternalProviderAsync_ReturnsUser_WhenMappingExists()
        {
            // Arrange
            var request = new UserCreationRequest
            {
                ExternalProvider = "test-provider",
                ExternalSubject = "external-subject-123",
                Email = "test@example.com",
                OrganizationId = _testOrganization.Id
            };

            await _userService.GetOrCreateUserAsync(request);

            // Act
            var user = await _userService.GetUserByExternalProviderAsync(
                request.ExternalProvider, request.ExternalSubject);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(_userService.HashEmail(request.Email), user.EmailHash);
        }

        [Fact]
        public async Task GetUserByExternalProviderAsync_ReturnsNull_WhenMappingDoesNotExist()
        {
            // Act
            var user = await _userService.GetUserByExternalProviderAsync(
                "provider", "subject");

            // Assert
            Assert.Null(user);
        }

        [Theory]
        [InlineData("test@example.com", "TEST@EXAMPLE.COM")]
        [InlineData("User@Domain.Com", "user@domain.com")]
        public void HashEmail_NormalizesEmailToLowerCase(string email1, string email2)
        {
            // Act
            var hash1 = _userService.HashEmail(email1);
            var hash2 = _userService.HashEmail(email2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashEmail_ProducesDifferentHashesForDifferentEmails()
        {
            // Arrange
            var email1 = "user1@example.com";
            var email2 = "user2@example.com";

            // Act
            var hash1 = _userService.HashEmail(email1);
            var hash2 = _userService.HashEmail(email2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }
    }
}