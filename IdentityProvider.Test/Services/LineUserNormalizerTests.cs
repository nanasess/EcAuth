using IdentityProvider.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityProvider.Test.Services
{
    public class LineUserNormalizerTests
    {
        private readonly Mock<ILogger<LineUserNormalizer>> _mockLogger;
        private readonly LineUserNormalizer _normalizer;

        public LineUserNormalizerTests()
        {
            _mockLogger = new Mock<ILogger<LineUserNormalizer>>();
            _normalizer = new LineUserNormalizer(_mockLogger.Object);
        }

        [Fact]
        public void ProviderName_ShouldReturnLine()
        {
            // Act
            var result = _normalizer.ProviderName;

            // Assert
            Assert.Equal("line", result);
        }

        [Fact]
        public void Normalize_ValidLineResponse_ShouldReturnExternalUserInfo()
        {
            // Arrange
            var lineResponse = new
            {
                userId = "line-user-123",
                displayName = "LINE User",
                pictureUrl = "https://example.com/line-photo.jpg",
                statusMessage = "Hello from LINE",
                language = "ja"
            };

            // Act
            var result = _normalizer.Normalize(lineResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("line-user-123", result.Subject);
            Assert.Null(result.Email); // LINEは基本的にメールを提供しない
            Assert.Equal("LINE User", result.Name);
            Assert.Equal("line", result.Provider);
            Assert.NotNull(result.Claims);
            Assert.True(result.Claims.ContainsKey("pictureUrl"));
            Assert.True(result.Claims.ContainsKey("statusMessage"));
            Assert.True(result.Claims.ContainsKey("language"));
        }

        [Fact]
        public void Normalize_MinimalLineResponse_ShouldReturnExternalUserInfo()
        {
            // Arrange
            var lineResponse = new
            {
                userId = "line-user-456"
            };

            // Act
            var result = _normalizer.Normalize(lineResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("line-user-456", result.Subject);
            Assert.Null(result.Email);
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal("line", result.Provider);
            Assert.NotNull(result.Claims);
        }

        [Fact]
        public void Normalize_LineResponseWithNullValues_ShouldHandleGracefully()
        {
            // Arrange
            var lineResponse = new
            {
                userId = "line-user-789",
                displayName = (string?)null,
                pictureUrl = (string?)null
            };

            // Act
            var result = _normalizer.Normalize(lineResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("line-user-789", result.Subject);
            Assert.Null(result.Email);
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal("line", result.Provider);
        }

        [Fact]
        public void Normalize_InvalidLineResponse_ShouldThrowInvalidOperationException()
        {
            // Arrange
            string? invalidResponse = null;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _normalizer.Normalize(invalidResponse));
            Assert.Contains("Failed to normalize LINE user information", exception.Message);
        }
    }
}