using System;
using System.Threading.Tasks;
using IdentityProvider.Models;
using IdentityProvider.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityProvider.Test.Services
{
    public class AuthorizationCodeServiceTests : IDisposable
    {
        private readonly EcAuthDbContext _context;
        private readonly AuthorizationCodeService _authCodeService;
        private readonly Mock<ILogger<AuthorizationCodeService>> _loggerMock;
        private readonly Client _testClient;
        private readonly EcAuthUser _testUser;

        public AuthorizationCodeServiceTests()
        {
            var options = new DbContextOptionsBuilder<EcAuthDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new EcAuthDbContext(options);
            _loggerMock = new Mock<ILogger<AuthorizationCodeService>>();
            _authCodeService = new AuthorizationCodeService(_context, _loggerMock.Object);

            // Setup test data
            var organization = new Organization
            {
                Id = "test-org",
                TenantId = "test-tenant",
                Name = "Test Organization"
            };
            _context.Organizations.Add(organization);

            _testClient = new Client
            {
                Id = "test-client-id",
                Secret = "test-secret",
                Name = "Test Client",
                OrganizationId = organization.Id
            };
            _context.Clients.Add(_testClient);

            _testUser = new EcAuthUser
            {
                Subject = Guid.NewGuid().ToString(),
                EmailHash = "test-hash",
                OrganizationId = 1
            };
            _context.EcAuthUsers.Add(_testUser);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GenerateAuthorizationCodeAsync_CreatesValidCode()
        {
            // Arrange
            var clientId = 1; // Using int based on actual implementation
            var ecAuthUserId = _testUser.Id;
            var redirectUri = "https://example.com/callback";
            var scope = "openid profile email";

            // Act
            var code = await _authCodeService.GenerateAuthorizationCodeAsync(
                clientId, ecAuthUserId, redirectUri, scope);

            // Assert
            Assert.NotNull(code);
            Assert.NotEmpty(code);

            // Verify code was saved to database
            var savedCode = await _context.AuthorizationCodes
                .FirstOrDefaultAsync(c => c.Code == code);
            Assert.NotNull(savedCode);
            Assert.Equal(clientId, savedCode.ClientId);
            Assert.Equal(ecAuthUserId, savedCode.EcAuthUserId);
            Assert.Equal(redirectUri, savedCode.RedirectUri);
            Assert.Equal(scope, savedCode.Scope);
            Assert.False(savedCode.IsUsed);
            Assert.True(savedCode.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task GenerateAuthorizationCodeAsync_CreatesUniqueCode()
        {
            // Arrange
            var clientId = 1;
            var ecAuthUserId = _testUser.Id;
            var redirectUri = "https://example.com/callback";

            // Act
            var code1 = await _authCodeService.GenerateAuthorizationCodeAsync(
                clientId, ecAuthUserId, redirectUri, null);
            var code2 = await _authCodeService.GenerateAuthorizationCodeAsync(
                clientId, ecAuthUserId, redirectUri, null);

            // Assert
            Assert.NotEqual(code1, code2);
        }

        [Fact]
        public async Task ValidateAuthorizationCodeAsync_ReturnsCode_WhenValid()
        {
            // Arrange
            var clientId = 1;
            var ecAuthUserId = _testUser.Id;
            var redirectUri = "https://example.com/callback";

            var code = await _authCodeService.GenerateAuthorizationCodeAsync(
                clientId, ecAuthUserId, redirectUri, null);

            // Act
            var validatedCode = await _authCodeService.ValidateAuthorizationCodeAsync(
                code, clientId, redirectUri);

            // Assert
            Assert.NotNull(validatedCode);
            Assert.Equal(clientId, validatedCode.ClientId);
            Assert.Equal(ecAuthUserId, validatedCode.EcAuthUserId);
        }

        [Fact]
        public async Task ValidateAuthorizationCodeAsync_ReturnsNull_WhenCodeDoesNotExist()
        {
            // Act
            var validatedCode = await _authCodeService.ValidateAuthorizationCodeAsync(
                "invalid-code", 1, "redirect-uri");

            // Assert
            Assert.Null(validatedCode);
        }

        [Fact]
        public async Task ValidateAuthorizationCodeAsync_ReturnsNull_WhenClientIdMismatch()
        {
            // Arrange
            var code = await _authCodeService.GenerateAuthorizationCodeAsync(
                1, _testUser.Id, "https://example.com/callback", null);

            // Act
            var validatedCode = await _authCodeService.ValidateAuthorizationCodeAsync(
                code, 2, "https://example.com/callback");

            // Assert
            Assert.Null(validatedCode);
        }

        [Fact]
        public async Task ValidateAuthorizationCodeAsync_ReturnsNull_WhenRedirectUriMismatch()
        {
            // Arrange
            var code = await _authCodeService.GenerateAuthorizationCodeAsync(
                1, _testUser.Id, "https://example.com/callback", null);

            // Act
            var validatedCode = await _authCodeService.ValidateAuthorizationCodeAsync(
                code, 1, "https://different.com/callback");

            // Assert
            Assert.Null(validatedCode);
        }

        [Fact]
        public async Task ValidateAuthorizationCodeAsync_ReturnsNull_WhenCodeIsUsed()
        {
            // Arrange
            var code = await _authCodeService.GenerateAuthorizationCodeAsync(
                1, _testUser.Id, "https://example.com/callback", null);
            
            // Mark as used
            await _authCodeService.MarkCodeAsUsedAsync(code);

            // Act
            var validatedCode = await _authCodeService.ValidateAuthorizationCodeAsync(
                code, 1, "https://example.com/callback");

            // Assert
            Assert.Null(validatedCode);
        }

        [Fact]
        public async Task ValidateAuthorizationCodeAsync_ReturnsNull_WhenCodeIsExpired()
        {
            // Arrange
            var authCode = new AuthorizationCode
            {
                Code = "expired-code",
                ClientId = 1,
                EcAuthUserId = _testUser.Id,
                RedirectUri = "https://example.com/callback",
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired 5 minutes ago
                IsUsed = false
            };
            _context.AuthorizationCodes.Add(authCode);
            await _context.SaveChangesAsync();

            // Act
            var validatedCode = await _authCodeService.ValidateAuthorizationCodeAsync(
                "expired-code", 1, "https://example.com/callback");

            // Assert
            Assert.Null(validatedCode);
        }

        [Fact]
        public async Task MarkCodeAsUsedAsync_MarksCodeAsUsed()
        {
            // Arrange
            var code = await _authCodeService.GenerateAuthorizationCodeAsync(
                1, _testUser.Id, "redirect", null);

            // Act
            await _authCodeService.MarkCodeAsUsedAsync(code);

            // Assert
            var savedCode = await _context.AuthorizationCodes
                .FirstOrDefaultAsync(c => c.Code == code);
            Assert.NotNull(savedCode);
            Assert.True(savedCode.IsUsed);
        }

        [Fact]
        public async Task MarkCodeAsUsedAsync_DoesNotThrow_WhenCodeDoesNotExist()
        {
            // Act & Assert - Should not throw
            await _authCodeService.MarkCodeAsUsedAsync("non-existent-code");
        }
    }
}