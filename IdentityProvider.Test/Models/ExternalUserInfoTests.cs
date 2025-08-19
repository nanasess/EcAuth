using IdentityProvider.Models;

namespace IdentityProvider.Test.Models
{
    public class ExternalUserInfoTests
    {
        [Fact]
        public void ExternalUserInfo_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var externalUserInfo = new ExternalUserInfo();

            // Assert
            Assert.Equal(string.Empty, externalUserInfo.Subject);
            Assert.Null(externalUserInfo.Email);
            Assert.Null(externalUserInfo.Name);
            Assert.Equal(string.Empty, externalUserInfo.Provider);
            Assert.Null(externalUserInfo.Claims);
        }

        [Fact]
        public void ExternalUserInfo_SetProperties_ShouldStoreValues()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "custom_claim", "value" },
                { "another_claim", 123 }
            };

            // Act
            var externalUserInfo = new ExternalUserInfo
            {
                Subject = "test-subject-123",
                Email = "test@example.com",
                Name = "Test User",
                Provider = "test-provider",
                Claims = claims
            };

            // Assert
            Assert.Equal("test-subject-123", externalUserInfo.Subject);
            Assert.Equal("test@example.com", externalUserInfo.Email);
            Assert.Equal("Test User", externalUserInfo.Name);
            Assert.Equal("test-provider", externalUserInfo.Provider);
            Assert.NotNull(externalUserInfo.Claims);
            Assert.Equal(2, externalUserInfo.Claims.Count);
            Assert.Equal("value", externalUserInfo.Claims["custom_claim"]);
            Assert.Equal(123, externalUserInfo.Claims["another_claim"]);
        }

        [Fact]
        public void ExternalUserInfo_WithNullEmailAndName_ShouldAllowNulls()
        {
            // Act
            var externalUserInfo = new ExternalUserInfo
            {
                Subject = "test-subject",
                Email = null,
                Name = null,
                Provider = "test-provider"
            };

            // Assert
            Assert.Equal("test-subject", externalUserInfo.Subject);
            Assert.Null(externalUserInfo.Email);
            Assert.Null(externalUserInfo.Name);
            Assert.Equal("test-provider", externalUserInfo.Provider);
        }
    }
}