using IdentityProvider.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityProvider.Test.Services
{
    public class GoogleUserNormalizerTests
    {
        private readonly Mock<ILogger<GoogleUserNormalizer>> _mockLogger;
        private readonly GoogleUserNormalizer _normalizer;

        public GoogleUserNormalizerTests()
        {
            _mockLogger = new Mock<ILogger<GoogleUserNormalizer>>();
            _normalizer = new GoogleUserNormalizer(_mockLogger.Object);
        }

        [Fact]
        public void ProviderName_ShouldReturnGoogle()
        {
            // Act
            var result = _normalizer.ProviderName;

            // Assert
            Assert.Equal("google", result);
        }

        [Fact]
        public void Normalize_ValidGoogleResponse_ShouldReturnExternalUserInfo()
        {
            // Arrange
            var googleResponse = new
            {
                sub = "google-user-123",
                email = "test@example.com",
                name = "Test User",
                email_verified = "true",
                given_name = "Test",
                family_name = "User",
                picture = "https://example.com/photo.jpg"
            };

            // Act
            var result = _normalizer.Normalize(googleResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("google-user-123", result.Subject);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("google", result.Provider);
            Assert.NotNull(result.Claims);
            Assert.True(result.Claims.ContainsKey("email_verified"));
            Assert.True(result.Claims.ContainsKey("given_name"));
            Assert.True(result.Claims.ContainsKey("family_name"));
            Assert.True(result.Claims.ContainsKey("picture"));
        }

        [Fact]
        public void Normalize_MinimalGoogleResponse_ShouldReturnExternalUserInfo()
        {
            // Arrange
            var googleResponse = new
            {
                sub = "google-user-456"
            };

            // Act
            var result = _normalizer.Normalize(googleResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("google-user-456", result.Subject);
            Assert.Equal(string.Empty, result.Email);
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal("google", result.Provider);
            Assert.NotNull(result.Claims);
        }

        [Fact]
        public void Normalize_GoogleResponseWithNullValues_ShouldHandleGracefully()
        {
            // Arrange
            var googleResponse = new
            {
                sub = "google-user-789",
                email = (string?)null,
                name = (string?)null
            };

            // Act
            var result = _normalizer.Normalize(googleResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("google-user-789", result.Subject);
            Assert.Equal(string.Empty, result.Email);
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal("google", result.Provider);
        }

        [Fact]
        public void Normalize_InvalidGoogleResponse_ShouldThrowInvalidOperationException()
        {
            // Arrange
            string? invalidResponse = null;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _normalizer.Normalize(invalidResponse));
            Assert.Contains("Failed to normalize Google user information", exception.Message);
        }
    }
}