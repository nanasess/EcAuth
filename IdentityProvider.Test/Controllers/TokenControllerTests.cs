using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityProvider.Controllers;
using IdentityProvider.Models;
using IdentityProvider.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityProvider.Test.Controllers
{
    public class TokenControllerTests : IDisposable
    {
        private readonly EcAuthDbContext _context;
        private readonly Mock<IHostEnvironment> _environmentMock;
        private readonly Mock<IAuthorizationCodeService> _authCodeServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<TokenController>> _loggerMock;
        private readonly TokenController _controller;
        private readonly RsaKeyPair _testKeyPair;
        private readonly Client _testClient;

        public TokenControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<EcAuthDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new EcAuthDbContext(options);

            // Setup mocks
            _environmentMock = new Mock<IHostEnvironment>();
            _authCodeServiceMock = new Mock<IAuthorizationCodeService>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<TokenController>>();

            // Create test client and RSA key pair
            _testClient = new Client
            {
                ClientId = "test-client",
                ClientSecret = "test-secret",
                AppName = "Test App"
            };
            
            _testKeyPair = new RsaKeyPair
            {
                PublicKey = GenerateTestPublicKey(),
                PrivateKey = GenerateTestPrivateKey(),
                Client = _testClient
            };
            
            _testClient.RsaKeyPair = _testKeyPair;

            // Setup test data
            SetupTestData();

            _controller = new TokenController(
                _context,
                _environmentMock.Object,
                _authCodeServiceMock.Object,
                _userServiceMock.Object,
                _loggerMock.Object);

            // Setup HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Request.Host = new HostString("localhost", 8081);
            _controller.HttpContext.Request.Scheme = "http";
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SetupTestData()
        {
            // Add test client and key pair
            _context.Clients.Add(_testClient);
            _context.RsaKeyPairs.Add(_testKeyPair);

            // Add test organization
            var organization = new Organization
            {
                Id = "test-org-id",
                TenantId = "test-tenant",
                Name = "Test Organization"
            };
            _context.Organizations.Add(organization);

            // Add test client
            var client = new Client
            {
                Id = "test-client-id",
                Secret = "test-client-secret",
                Name = "Test Client",
                OrganizationId = organization.Id
            };
            _context.Clients.Add(client);

            // Add redirect URI
            var redirectUri = new RedirectUri
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Uri = "https://example.com/callback"
            };
            _context.RedirectUris.Add(redirectUri);

            _context.SaveChanges();
        }

        [Fact]
        public async Task Token_ReturnsError_WhenGrantTypeNotAuthorizationCode()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "client_credentials",
                ["code"] = "test-code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "test-client-id",
                ["client_secret"] = "test-client-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            // Act
            var result = await _controller.Token(
                "client_credentials", "test-code", "https://example.com/callback", 
                "test-client-id", "test-client-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic error = jsonResult.Value!;
            Assert.Equal("unsupported_grant_type", (string)error.error);
        }

        [Fact]
        public async Task Token_ReturnsError_WhenCodeMissing()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "test-client-id",
                ["client_secret"] = "test-client-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            // Act
            var result = await _controller.Token(
                "authorization_code", "", "https://example.com/callback",
                "test-client-id", "test-client-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic error = jsonResult.Value!;
            Assert.Equal("invalid_request", (string)error.error);
        }

        [Fact]
        public async Task Token_ReturnsError_WhenClientNotFound()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = "test-code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "non-existent-client",
                ["client_secret"] = "wrong-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            // Act
            var result = await _controller.Token(
                "authorization_code", "test-code", "https://example.com/callback",
                "non-existent-client", "wrong-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic error = jsonResult.Value!;
            Assert.Equal("invalid_client", (string)error.error);
        }

        [Fact]
        public async Task Token_ReturnsError_WhenClientSecretInvalid()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = "test-code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "test-client-id",
                ["client_secret"] = "wrong-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            // Act
            var result = await _controller.Token(
                "authorization_code", "test-code", "https://example.com/callback",
                "test-client-id", "wrong-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic error = jsonResult.Value!;
            Assert.Equal("invalid_client", (string)error.error);
        }

        [Fact]
        public async Task Token_ReturnsError_WhenCodeInvalid()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = "invalid-code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "test-client-id",
                ["client_secret"] = "test-client-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            _authCodeServiceMock
                .Setup(x => x.ValidateAuthorizationCodeAsync(
                    "invalid-code", It.IsAny<int>(), "https://example.com/callback"))
                .ReturnsAsync((AuthorizationCode?)null);

            // Act
            var result = await _controller.Token(
                "authorization_code", "invalid-code", "https://example.com/callback",
                "test-client-id", "test-client-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic error = jsonResult.Value!;
            Assert.Equal("invalid_grant", (string)error.error);
        }

        [Fact]
        public async Task Token_ReturnsTokens_WhenRequestValid()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = "valid-code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "test-client-id",
                ["client_secret"] = "test-client-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            var authCode = new AuthorizationCode
            {
                Code = "valid-code",
                ClientId = 1, // This should match a real client ID in the database
                EcAuthUserId = 1,
                RedirectUri = "https://example.com/callback",
                Scope = "openid profile email",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _authCodeServiceMock
                .Setup(x => x.ValidateAuthorizationCodeAsync(
                    "valid-code", It.IsAny<int>(), "https://example.com/callback"))
                .ReturnsAsync(authCode);

            var user = new EcAuthUser
            {
                Id = 1,
                Subject = "user-subject",
                EmailHash = "hash",
                OrganizationId = 1
            };

            _userServiceMock
                .Setup(x => x.GetUserBySubjectAsync(user.Subject))
                .ReturnsAsync(user);

            _authCodeServiceMock
                .Setup(x => x.MarkCodeAsUsedAsync("valid-code"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Token(
                "authorization_code", "valid-code", "https://example.com/callback",
                "test-client-id", "test-client-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic response = jsonResult.Value!;
            
            Assert.NotNull(response.access_token);
            Assert.NotNull(response.id_token);
            Assert.Equal("Bearer", (string)response.token_type);
            Assert.Equal(3600, (int)response.expires_in);
            Assert.Equal("openid profile email", (string)response.scope);

            // Verify services were called
            _authCodeServiceMock.Verify(x => x.ValidateAuthorizationCodeAsync(
                "valid-code", It.IsAny<int>(), "https://example.com/callback"), Times.Once);
            _userServiceMock.Verify(x => x.GetUserBySubjectAsync(user.Subject), Times.Once);
            _authCodeServiceMock.Verify(x => x.MarkCodeAsUsedAsync("valid-code"), Times.Once);
        }

        [Fact]
        public async Task Token_HandlesOptionalScopes_WhenNotRequested()
        {
            // Arrange
            var formData = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = "valid-code",
                ["redirect_uri"] = "https://example.com/callback",
                ["client_id"] = "test-client-id",
                ["client_secret"] = "test-client-secret"
            });
            _controller.HttpContext.Request.Form = formData;

            var authCode = new AuthorizationCode
            {
                Code = "valid-code",
                ClientId = 1,
                EcAuthUserId = 1,
                RedirectUri = "https://example.com/callback",
                Scope = "openid", // Only openid scope
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _authCodeServiceMock
                .Setup(x => x.ValidateAuthorizationCodeAsync(
                    "valid-code", It.IsAny<int>(), "https://example.com/callback"))
                .ReturnsAsync(authCode);

            var user = new EcAuthUser
            {
                Id = 1,
                Subject = "user-subject",
                EmailHash = "hash",
                OrganizationId = 1
            };

            _userServiceMock
                .Setup(x => x.GetUserBySubjectAsync(user.Subject))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Token(
                "authorization_code", "valid-code", "https://example.com/callback",
                "test-client-id", "test-client-secret");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic response = jsonResult.Value!;
            
            Assert.NotNull(response.id_token);
            Assert.Equal("openid", (string)response.scope);
        }

        private string GenerateTestPublicKey()
        {
            // This is a test RSA public key in PEM format
            return @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAu1SU1LfVLPHCozMxH2Mo
4lgOEePzNm0tRgeLezV6ffAt0gunVTLw7onLRnrq0/IzW7yWR7QkrmBL7jTKEn5u
+qKhbwKfBstIs+bMY2Zkp18gnTxKLxoS2tFczGkPLPgizskuemMghRniWaoLcyeh
kd3qqGElvW/VDL5AaWTg0nLVkjRo9z+40RQzuVaE8AkAFmxZzow3x+VJYKdjykkJ
0iT9wCS0DRTXu269V264Vf/3jvredZiKRkgwlL9xNAwxXFg0x/XFw005UWVRIkdg
cKWTjpBP2dPwVZ4WWC+9aGVd+Gyn1o0CLelf4rEjGoXbAAEgAqeGUxrcIlbjXfbc
mwIDAQAB
-----END PUBLIC KEY-----";
        }

        private string GenerateTestPrivateKey()
        {
            // This is a test RSA private key in PEM format
            return @"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAu1SU1LfVLPHCozMxH2Mo4lgOEePzNm0tRgeLezV6ffAt0gun
VTLw7onLRnrq0/IzW7yWR7QkrmBL7jTKEn5u+qKhbwKfBstIs+bMY2Zkp18gnTxK
LxoS2tFczGkPLPgizskuemMghRniWaoLcyehkd3qqGElvW/VDL5AaWTg0nLVkjRo
9z+40RQzuVaE8AkAFmxZzow3x+VJYKdjykkJ0iT9wCS0DRTXu269V264Vf/3jvre
dZiKRkgwlL9xNAwxXFg0x/XFw005UWVRIkdgcKWTjpBP2dPwVZ4WWC+9aGVd+Gyn
1o0CLelf4rEjGoXbAAEgAqeGUxrcIlbjXfbcmwIDAQABAoIBACiARq2wkltjtcjs
kFvZ7w1JAORHbEufEO1Eu27zOIlqbgyAcAl7q+/1bip4Z/x1IVES84/yTaM8p0go
amMhvgry/mS8vNi1BN2SAZEnb/7xSxbflb70bX9RHLJqKnp5GZe2jexw+wyXlwaM
+bclUCrh9e1ltH7IvUrRrQnFJfh+is1fRon9Co9Li0GwoN0x0byrrngU8Ak3Y6D9
D8GjQA4Elm94ST3izJv8iCOLSDBmzsPsXfcCUZfmTfZ5DbUDMbMxRnSo3nQeoKGC
0Lj9FkWcfmLcpGlSXTO+Ww1L7EGq+PT3NtRae1FZPwjddQ1/4V905kyQFLamAA5Y
lSpE2wkCgYEAy1OPLQcZt4NQnQzPz2SBJqQN2P5u3vXl+zNVKP8w4eBv0vWuJJF+
hkGNnSxXQrTkvDOIUddSKOzHHgSg4nY6K02ecyT0PPm/UZvtRpWrnBjcEVtHEJNp
bU9pLD5iZ0J9sbzPU/LxPmuAP2Bs8JmTn6aFRspFrP7W0s1Nmk2jsm0CgYEA7WwI
UVeRkzHMFJmm9dv0IaKJqkYRUHgvJjZ1AuK3z0hT7jEsSGi6+8XHKP0ZqM8MdPZX
E/hROXrIwg9FfFFMQysAm1qyW6+pQZ2W9QdqYJLrcTQQ4VvnAYreUeqVvDUqnDy5
uAuAjCkqHKkrZjLNHqmGJqPbmyc3VIgEaGVpYlcCgYAdy5xz8qrPXHh+XYapQsrO
Ss7cHqH2TXSdNPR2q6LjqSFwmY5h8nGb9F1w5J6L2nrHMkrAe+wgIk8vcKLjjPRr
hgmAQdVVoFCCJnBF7T3vubZKwBn5Kc2VdYpNMC6W+wjX5qHGF8cH5zEC7EYYmsay
SfIK6rVWHl1wXN7F7qxqKQKBgBzwq1lc1y1dHCxj7KcmY7QPVD24nkfEZcvu4M06
YjAD/ksf0D6iAcQVV29rJ+MsHXNZQbB8YNP3i5Qc0y7NL7cNRQiPnhZjBtVqpQNC
f8Y2g6KhT9E8E3darWssLpANNL8DvGpFBmGwftnPEzPS6qkMxT1hFBN7vT2XwMUn
VcNzAoGAXCJTdPX72pNVLEHIIbWJh1hPVqgFxD5HAYuHPPsxe/OWjd1qfAKgQPXR
A6CZn3RJ4J+dmt5DylCqELpnVGsBhTkUNqAHqYXmvOPelLrm8buFFMUQQh2RI0U0
UtWvu+lzGV9oIKNLXnX4L5s/UZXEb5VHFWXMXGLh0D3vcVOyFe0=
-----END RSA PRIVATE KEY-----";
        }
    }
}