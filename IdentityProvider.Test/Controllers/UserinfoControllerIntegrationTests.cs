using IdentityProvider.Controllers;
using IdentityProvider.Models;
using IdentityProvider.Services;
using IdentityProvider.Test.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityProvider.Test.Controllers
{
    public class UserinfoControllerIntegrationTests : IDisposable
    {
        private readonly EcAuthDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly Mock<ILogger<UserinfoController>> _mockLogger;
        private readonly UserinfoController _controller;
        private readonly MockTenantService _mockTenantService;

        public UserinfoControllerIntegrationTests()
        {
            _context = TestDbContextHelper.CreateInMemoryContext();
            _mockTenantService = new MockTenantService();

            // 実際のサービスのインスタンスを作成（統合テスト）
            var mockLogger = new Mock<ILogger<TokenService>>();
            var mockUserLogger = new Mock<ILogger<UserService>>();

            _tokenService = new TokenService(_context, mockLogger.Object);
            _userService = new UserService(_context, mockUserLogger.Object);

            _mockLogger = new Mock<ILogger<UserinfoController>>();

            _controller = new UserinfoController(
                _tokenService,
                _userService,
                _mockLogger.Object);

            // HttpContext のセットアップ
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task IntegrationTest_FullWorkflow_ValidAccessToken_ReturnsUserInfo()
        {
            // Arrange
            await SeedTestDataAsync();

            var accessToken = "integration-test-token";
            var subject = "integration-test-subject";

            // アクセストークンをデータベースに直接挿入
            var tokenEntity = new AccessToken
            {
                Token = accessToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                ClientId = 1,
                EcAuthSubject = subject,
                CreatedAt = DateTime.UtcNow,
                Scopes = "openid profile"
            };
            _context.AccessTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {accessToken}";

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var subProperty = response.GetType().GetProperty("sub")?.GetValue(response);
            Assert.Equal(subject, subProperty);
        }

        [Fact]
        public async Task IntegrationTest_ExpiredAccessToken_ReturnsUnauthorized()
        {
            // Arrange
            await SeedTestDataAsync();

            var accessToken = "expired-test-token";
            var subject = "integration-test-subject";

            // 期限切れのアクセストークンをデータベースに直接挿入
            var tokenEntity = new AccessToken
            {
                Token = accessToken,
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // 期限切れ
                ClientId = 1,
                EcAuthSubject = subject,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Scopes = "openid profile"
            };
            _context.AccessTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {accessToken}";

            // Act
            var result = await _controller.Get();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            Assert.NotNull(response);

            var errorProperty = response.GetType().GetProperty("error")?.GetValue(response);
            Assert.Equal("invalid_token", errorProperty);
        }

        [Fact]
        public async Task IntegrationTest_MultiTenant_CorrectUserForTenant()
        {
            // Arrange
            await SeedMultiTenantDataAsync();

            var accessToken = "tenant1-token";
            var subject = "tenant1-user-subject";

            // テナント1のユーザー用アクセストークン
            var tokenEntity = new AccessToken
            {
                Token = accessToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                ClientId = 1, // Tenant1のクライアント
                EcAuthSubject = subject,
                CreatedAt = DateTime.UtcNow,
                Scopes = "openid profile"
            };
            _context.AccessTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            // テナント1に切り替え
            _mockTenantService.SetTenant("tenant1");

            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {accessToken}";

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var subProperty = response.GetType().GetProperty("sub")?.GetValue(response);
            Assert.Equal(subject, subProperty);
        }

        [Fact]
        public async Task IntegrationTest_CrossTenantAccess_ReturnsUnauthorized()
        {
            // Arrange
            await SeedMultiTenantDataAsync();

            var accessToken = "tenant1-token";
            var subject = "tenant1-user-subject";

            // テナント1のユーザー用アクセストークン
            var tokenEntity = new AccessToken
            {
                Token = accessToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                ClientId = 1, // Tenant1のクライアント
                EcAuthSubject = subject,
                CreatedAt = DateTime.UtcNow,
                Scopes = "openid profile"
            };
            _context.AccessTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            // テナント2に切り替え（クロステナントアクセス）
            _mockTenantService.SetTenant("tenant2");

            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {accessToken}";

            // Act
            var result = await _controller.Get();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            Assert.NotNull(response);

            var errorProperty = response.GetType().GetProperty("error")?.GetValue(response);
            Assert.Equal("invalid_token", errorProperty);
        }

        [Fact]
        public async Task IntegrationTest_RevokedAccessToken_ReturnsUnauthorized()
        {
            // Arrange
            await SeedTestDataAsync();

            var accessToken = "revoked-test-token";
            var subject = "integration-test-subject";

            // アクセストークンをデータベースに追加
            var tokenEntity = new AccessToken
            {
                Token = accessToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                ClientId = 1,
                EcAuthSubject = subject,
                CreatedAt = DateTime.UtcNow,
                Scopes = "openid profile"
            };
            _context.AccessTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            // トークンを無効化（削除）
            await _tokenService.RevokeAccessTokenAsync(accessToken);

            _controller.HttpContext.Request.Headers["Authorization"] = $"Bearer {accessToken}";

            // Act
            var result = await _controller.Get();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            Assert.NotNull(response);

            var errorProperty = response.GetType().GetProperty("error")?.GetValue(response);
            Assert.Equal("invalid_token", errorProperty);
        }

        private async Task SeedTestDataAsync()
        {
            var organization = new Organization
            {
                Id = 1,
                Code = "test-org",
                Name = "Test Organization",
                TenantName = "test-tenant",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.Organizations.Add(organization);

            var client = new Client
            {
                Id = 1,
                ClientId = "test-client",
                ClientSecret = "test-secret",
                AppName = "Test App",
                OrganizationId = 1
            };
            _context.Clients.Add(client);

            var user = new EcAuthUser
            {
                Subject = "integration-test-subject",
                EmailHash = "integration-test-email-hash",
                OrganizationId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.EcAuthUsers.Add(user);

            await _context.SaveChangesAsync();
        }

        private async Task SeedMultiTenantDataAsync()
        {
            // テナント1
            var org1 = new Organization
            {
                Id = 1,
                Code = "tenant1-org",
                Name = "Tenant 1 Organization",
                TenantName = "tenant1",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.Organizations.Add(org1);

            var client1 = new Client
            {
                Id = 1,
                ClientId = "tenant1-client",
                ClientSecret = "tenant1-secret",
                AppName = "Tenant 1 App",
                OrganizationId = 1
            };
            _context.Clients.Add(client1);

            var user1 = new EcAuthUser
            {
                Subject = "tenant1-user-subject",
                EmailHash = "tenant1-email-hash",
                OrganizationId = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.EcAuthUsers.Add(user1);

            // テナント2
            var org2 = new Organization
            {
                Id = 2,
                Code = "tenant2-org",
                Name = "Tenant 2 Organization",
                TenantName = "tenant2",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.Organizations.Add(org2);

            var client2 = new Client
            {
                Id = 2,
                ClientId = "tenant2-client",
                ClientSecret = "tenant2-secret",
                AppName = "Tenant 2 App",
                OrganizationId = 2
            };
            _context.Clients.Add(client2);

            var user2 = new EcAuthUser
            {
                Subject = "tenant2-user-subject",
                EmailHash = "tenant2-email-hash",
                OrganizationId = 2,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.EcAuthUsers.Add(user2);

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}