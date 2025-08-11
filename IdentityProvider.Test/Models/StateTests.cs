using IdentityProvider.Models;

namespace IdentityProvider.Test.Models
{
    public class StateTests
    {
        [Fact]
        public void State_DefaultValues_ShouldBeSetCorrectly()
        {
            var state = new State();

            Assert.Equal(0, state.OpenIdProviderId);
            Assert.Equal(string.Empty, state.RedirectUri);
            Assert.Equal(0, state.ClientId);
            Assert.Equal(0, state.OrganizationId);
            Assert.Null(state.Scope);
        }

        [Fact]
        public void State_SetProperties_ShouldRetainValues()
        {
            var openIdProviderId = 1;
            var redirectUri = "https://example.com/callback";
            var clientId = 2;
            var organizationId = 3;
            var scope = "openid profile";

            var state = new State
            {
                OpenIdProviderId = openIdProviderId,
                RedirectUri = redirectUri,
                ClientId = clientId,
                OrganizationId = organizationId,
                Scope = scope
            };

            Assert.Equal(openIdProviderId, state.OpenIdProviderId);
            Assert.Equal(redirectUri, state.RedirectUri);
            Assert.Equal(clientId, state.ClientId);
            Assert.Equal(organizationId, state.OrganizationId);
            Assert.Equal(scope, state.Scope);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public void State_ClientId_ShouldAcceptValidValues(int clientId)
        {
            var state = new State { ClientId = clientId };
            
            Assert.Equal(clientId, state.ClientId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public void State_OrganizationId_ShouldAcceptValidValues(int organizationId)
        {
            var state = new State { OrganizationId = organizationId };
            
            Assert.Equal(organizationId, state.OrganizationId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("openid")]
        [InlineData("openid profile")]
        [InlineData("openid profile email")]
        [InlineData("openid profile email phone")]
        [InlineData(null)]
        public void State_Scope_ShouldAcceptValidValues(string? scope)
        {
            var state = new State { Scope = scope };
            
            Assert.Equal(scope, state.Scope);
        }

        [Theory]
        [InlineData("")]
        [InlineData("https://example.com/callback")]
        [InlineData("https://localhost:3000/auth/callback")]
        [InlineData("http://dev.example.com/oauth/callback")]
        [InlineData("https://app.example.com/auth/callback?param=value")]
        public void State_RedirectUri_ShouldAcceptValidValues(string redirectUri)
        {
            var state = new State { RedirectUri = redirectUri };
            
            Assert.Equal(redirectUri, state.RedirectUri);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public void State_OpenIdProviderId_ShouldAcceptValidValues(int openIdProviderId)
        {
            var state = new State { OpenIdProviderId = openIdProviderId };
            
            Assert.Equal(openIdProviderId, state.OpenIdProviderId);
        }

        [Fact]
        public void State_AllProperties_ShouldWorkTogether()
        {
            var state = new State
            {
                OpenIdProviderId = 1,
                RedirectUri = "https://example.com/callback",
                ClientId = 2,
                OrganizationId = 3,
                Scope = "openid profile email"
            };

            // 全プロパティが独立して動作することを確認
            Assert.Equal(1, state.OpenIdProviderId);
            Assert.Equal("https://example.com/callback", state.RedirectUri);
            Assert.Equal(2, state.ClientId);
            Assert.Equal(3, state.OrganizationId);
            Assert.Equal("openid profile email", state.Scope);

            // 個別に変更しても他に影響しないことを確認
            state.ClientId = 10;
            Assert.Equal(10, state.ClientId);
            Assert.Equal(1, state.OpenIdProviderId);
            Assert.Equal(3, state.OrganizationId);

            state.OrganizationId = 20;
            Assert.Equal(20, state.OrganizationId);
            Assert.Equal(10, state.ClientId);
            Assert.Equal(1, state.OpenIdProviderId);

            state.Scope = "openid";
            Assert.Equal("openid", state.Scope);
            Assert.Equal("https://example.com/callback", state.RedirectUri);
        }

        [Fact]
        public void State_NewProperties_ShouldNotBreakExistingFunctionality()
        {
            // 既存のプロパティの動作が新しいプロパティの追加で影響を受けないことを確認
            var state = new State
            {
                OpenIdProviderId = 1,
                RedirectUri = "https://example.com/callback"
            };

            // 新しいプロパティがデフォルト値であることを確認
            Assert.Equal(0, state.ClientId);
            Assert.Equal(0, state.OrganizationId);
            Assert.Null(state.Scope);

            // 既存のプロパティが正常に動作することを確認
            Assert.Equal(1, state.OpenIdProviderId);
            Assert.Equal("https://example.com/callback", state.RedirectUri);
        }
    }
}