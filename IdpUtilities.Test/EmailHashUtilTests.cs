using IdpUtilities;

namespace IdpUtilities.Test
{
    public class EmailHashUtilTests
    {
        [Fact]
        public void HashEmail_ValidEmail_ShouldReturnConsistentHash()
        {
            var email = "test@example.com";
            
            var hash1 = EmailHashUtil.HashEmail(email);
            var hash2 = EmailHashUtil.HashEmail(email);
            
            Assert.Equal(hash1, hash2);
            Assert.NotEmpty(hash1);
        }

        [Fact]
        public void HashEmail_SameEmailDifferentCase_ShouldReturnSameHash()
        {
            var email1 = "Test@Example.Com";
            var email2 = "test@example.com";
            
            var hash1 = EmailHashUtil.HashEmail(email1);
            var hash2 = EmailHashUtil.HashEmail(email2);
            
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashEmail_WithWhitespace_ShouldTrimAndHash()
        {
            var email1 = "  test@example.com  ";
            var email2 = "test@example.com";
            
            var hash1 = EmailHashUtil.HashEmail(email1);
            var hash2 = EmailHashUtil.HashEmail(email2);
            
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashEmail_DifferentEmails_ShouldReturnDifferentHashes()
        {
            var email1 = "test1@example.com";
            var email2 = "test2@example.com";
            
            var hash1 = EmailHashUtil.HashEmail(email1);
            var hash2 = EmailHashUtil.HashEmail(email2);
            
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HashEmail_InvalidInput_ShouldThrowArgumentException(string? email)
        {
            Assert.Throws<ArgumentException>(() => EmailHashUtil.HashEmail(email!));
        }

        [Fact]
        public void HashEmail_ValidEmail_ShouldReturnHexString()
        {
            var email = "test@example.com";
            
            var hash = EmailHashUtil.HashEmail(email);
            
            Assert.True(hash.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F')));
        }

        [Fact]
        public void HashEmail_ValidEmail_ShouldReturn64CharacterHash()
        {
            var email = "test@example.com";
            
            var hash = EmailHashUtil.HashEmail(email);
            
            Assert.Equal(64, hash.Length);
        }

        [Theory]
        [InlineData("user@domain.com")]
        [InlineData("very.long.email.address@very.long.domain.name.com")]
        [InlineData("simple@test.co")]
        [InlineData("user+tag@example.org")]
        public void HashEmail_VariousValidEmails_ShouldProduceValidHashes(string email)
        {
            var hash = EmailHashUtil.HashEmail(email);
            
            Assert.NotEmpty(hash);
            Assert.Equal(64, hash.Length);
            Assert.True(hash.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F')));
        }
    }
}