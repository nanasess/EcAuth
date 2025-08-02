using IdentityProvider.Models;

namespace IdentityProvider.Test.Models
{
    public class ExternalIdpMappingTests
    {
        [Fact]
        public void ExternalIdpMapping_DefaultValues_ShouldBeSetCorrectly()
        {
            var mapping = new ExternalIdpMapping();

            Assert.Equal(0, mapping.Id);
            Assert.Equal(string.Empty, mapping.EcAuthSubject);
            Assert.Equal(string.Empty, mapping.ExternalProvider);
            Assert.Equal(string.Empty, mapping.ExternalSubject);
            Assert.True(mapping.CreatedAt <= DateTimeOffset.UtcNow);
            Assert.Null(mapping.EcAuthUser);
        }

        [Fact]
        public void ExternalIdpMapping_SetProperties_ShouldRetainValues()
        {
            var id = 123;
            var ecAuthSubject = "test-subject-uuid";
            var externalProvider = "google";
            var externalSubject = "external-subject-123";
            var createdAt = DateTimeOffset.UtcNow.AddDays(-1);

            var mapping = new ExternalIdpMapping
            {
                Id = id,
                EcAuthSubject = ecAuthSubject,
                ExternalProvider = externalProvider,
                ExternalSubject = externalSubject,
                CreatedAt = createdAt
            };

            Assert.Equal(id, mapping.Id);
            Assert.Equal(ecAuthSubject, mapping.EcAuthSubject);
            Assert.Equal(externalProvider, mapping.ExternalProvider);
            Assert.Equal(externalSubject, mapping.ExternalSubject);
            Assert.Equal(createdAt, mapping.CreatedAt);
        }

        [Theory]
        [InlineData("google")]
        [InlineData("line")]
        [InlineData("amazon")]
        [InlineData("microsoft")]
        public void ExternalIdpMapping_ExternalProvider_ShouldAcceptValidProviders(string provider)
        {
            var mapping = new ExternalIdpMapping { ExternalProvider = provider };
            
            Assert.Equal(provider, mapping.ExternalProvider);
        }

        [Theory]
        [InlineData("")]
        [InlineData("test-subject")]
        [InlineData("uuid-12345")]
        public void ExternalIdpMapping_EcAuthSubject_ShouldAcceptValidValues(string subject)
        {
            var mapping = new ExternalIdpMapping { EcAuthSubject = subject };
            
            Assert.Equal(subject, mapping.EcAuthSubject);
        }

        [Theory]
        [InlineData("")]
        [InlineData("external-id-123")]
        [InlineData("google-subject-456")]
        public void ExternalIdpMapping_ExternalSubject_ShouldAcceptValidValues(string externalSubject)
        {
            var mapping = new ExternalIdpMapping { ExternalSubject = externalSubject };
            
            Assert.Equal(externalSubject, mapping.ExternalSubject);
        }

        [Fact]
        public void ExternalIdpMapping_EcAuthUserRelation_ShouldWork()
        {
            var user = new EcAuthUser { Subject = "test-subject" };
            var mapping = new ExternalIdpMapping { EcAuthUser = user };
            
            Assert.Equal(user, mapping.EcAuthUser);
        }
    }
}