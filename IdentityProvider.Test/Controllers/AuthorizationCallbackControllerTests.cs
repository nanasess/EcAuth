using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityProvider.Controllers;
using IdentityProvider.Models;
using IdentityProvider.Services;
using IdentityProvider.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityProvider.Test.Controllers
{
    public class AuthorizationCallbackControllerTests : IDisposable
    {
        private readonly EcAuthDbContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IExternalIdpService> _externalIdpServiceMock;
        private readonly Mock<IAuthorizationCodeService> _authCodeServiceMock;
        private readonly Mock<ILogger<AuthorizationCallbackController>> _loggerMock;
        private readonly AuthorizationCallbackController _controller;
        private readonly ServiceCollection _services;
        private readonly ServiceProvider _serviceProvider;

        public AuthorizationCallbackControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<EcAuthDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new EcAuthDbContext(options);

            // Setup mocks
            _userServiceMock = new Mock<IUserService>();
            _externalIdpServiceMock = new Mock<IExternalIdpService>();
            _authCodeServiceMock = new Mock<IAuthorizationCodeService>();
            _loggerMock = new Mock<ILogger<AuthorizationCallbackController>>();

            // Setup services for HttpContext.RequestServices
            _services = new ServiceCollection();
            _services.AddSingleton(_externalIdpServiceMock.Object);
            _services.AddSingleton(_authCodeServiceMock.Object);
            _serviceProvider = _services.BuildServiceProvider();

            // Create controller
            _controller = new AuthorizationCallbackController(
                _context,
                _userServiceMock.Object,
                _loggerMock.Object);

            // Setup HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = _serviceProvider
                }
            };
        }

        public void Dispose()
        {
            _context.Dispose();
            _serviceProvider.Dispose();
        }

        [Fact]
        public async Task Index_Get_ReturnsErrorView_WhenErrorParameterPresent()
        {
            // Act
            var result = await _controller.Index("test-code", "test-state", null, "access_denied", "User denied access");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsType<Dictionary<string, string>>(viewResult.Model);
            Assert.Equal("access_denied", model["error"]);
            Assert.Equal("User denied access", model["error_description"]);
        }

        [Fact]
        public async Task Index_Get_ReturnsErrorView_WhenCodeMissing()
        {
            // Act
            var result = await _controller.Index(null, "test-state", null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsType<Dictionary<string, string>>(viewResult.Model);
            Assert.Equal("invalid_request", model["error"]);
        }

        [Fact]
        public async Task Index_Get_ReturnsErrorView_WhenStateMissing()
        {
            // Act
            var result = await _controller.Index("test-code", null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsType<Dictionary<string, string>>(viewResult.Model);
            Assert.Equal("invalid_request", model["error"]);
        }

        [Fact]
        public async Task Index_Post_CreatesUserAndRedirects_WhenValidRequest()
        {
            // Arrange
            var organizationId = 1;
            var providerId = "test-provider";
            var clientId = "test-client";
            var redirectUri = "https://example.com/callback";
            var scope = "openid profile email";

            // Create sealed state
            var stateData = new
            {
                provider = providerId,
                organizationId = organizationId,
                clientId = clientId,
                redirectUri = redirectUri,
                originalState = "original-state"
            };
            var sealedState = await IronTestHelper.SealStateAsync(stateData);

            // Setup mocks
            var externalUserInfo = new ExternalUserInfo
            {
                Subject = "external-subject",
                Email = "test@example.com",
                Name = "Test User"
            };
            _externalIdpServiceMock
                .Setup(x => x.GetExternalUserInfoAsync(
                    providerId, "test-code", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(externalUserInfo);

            var ecAuthUser = new EcAuthUser
            {
                Id = 1,
                Subject = "ecauth-subject",
                EmailHash = "hash",
                OrganizationId = organizationId
            };
            _userServiceMock
                .Setup(x => x.GetOrCreateUserAsync(It.IsAny<UserCreationRequest>()))
                .ReturnsAsync(ecAuthUser);

            _authCodeServiceMock
                .Setup(x => x.GenerateAuthorizationCodeAsync(
                    It.IsAny<int>(), ecAuthUser.Id, redirectUri, scope))
                .ReturnsAsync("new-auth-code");

            // Create form data
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["code"] = "test-code",
                ["state"] = sealedState,
                ["scope"] = scope
            });
            _controller.HttpContext.Request.Form = formCollection;

            // Act
            var result = await _controller.Index("test-code", sealedState, scope);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.StartsWith("https://example.com/callback?code=new-auth-code&state=original-state", redirectResult.Url);
            
            // Verify services were called
            _externalIdpServiceMock.Verify(x => x.GetExternalUserInfoAsync(
                providerId, "test-code", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _userServiceMock.Verify(x => x.GetOrCreateUserAsync(
                It.Is<UserCreationRequest>(r => 
                    r.ExternalProvider == providerId &&
                    r.ExternalSubject == externalUserInfo.Subject &&
                    r.Email == externalUserInfo.Email &&
                    r.OrganizationId == organizationId)), Times.Once);
            _authCodeServiceMock.Verify(x => x.GenerateAuthorizationCodeAsync(
                It.IsAny<int>(), ecAuthUser.Id, redirectUri, scope), Times.Once);
        }

        [Fact]
        public async Task Index_Post_ReturnsErrorView_WhenStateInvalid()
        {
            // Arrange
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["code"] = "test-code",
                ["state"] = "invalid-state",
                ["scope"] = "openid"
            });
            _controller.HttpContext.Request.Form = formCollection;

            // Act
            var result = await _controller.Index("test-code", "invalid-state", "openid");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsType<Dictionary<string, string>>(viewResult.Model);
            Assert.Equal("invalid_request", model["error"]);
        }

        [Fact]
        public async Task Index_Post_ReturnsErrorView_WhenExternalIdpServiceThrows()
        {
            // Arrange
            var stateData = new
            {
                provider = "test-provider",
                organizationId = 1,
                clientId = "test-client",
                redirectUri = "https://example.com/callback",
                originalState = "original-state"
            };
            var sealedState = await IronTestHelper.SealStateAsync(stateData);

            _externalIdpServiceMock
                .Setup(x => x.GetExternalUserInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("External IdP error"));

            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["code"] = "test-code",
                ["state"] = sealedState,
                ["scope"] = "openid"
            });
            _controller.HttpContext.Request.Form = formCollection;

            // Act
            var result = await _controller.Index("test-code", sealedState, "openid");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsType<Dictionary<string, string>>(viewResult.Model);
            Assert.Equal("server_error", model["error"]);
        }
    }
}