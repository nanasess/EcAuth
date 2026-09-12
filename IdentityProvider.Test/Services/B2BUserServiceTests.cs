using IdentityProvider.Models;
using IdentityProvider.Services;
using IdentityProvider.Test.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityProvider.Test.Services
{
    public class B2BUserServiceTests : IDisposable
    {
        /// <summary>
        /// テスト用の発行元識別子（EcAuthDocs#110）。実運用と同じく client_id から構成する。
        /// </summary>
        private const string TestIssuerKey = "client:test-client";

        private readonly EcAuthDbContext _context;
        private readonly B2BUserService _service;
        private readonly Mock<ILogger<B2BUserService>> _mockLogger;
        private readonly Organization _organization;

        public B2BUserServiceTests()
        {
            _context = TestDbContextHelper.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<B2BUserService>>();
            _service = new B2BUserService(_context, _mockLogger.Object);

            // テスト用のテナントをセットアップ
            _organization = new Organization
            {
                Id = 1,
                Code = "test-org",
                Name = "テスト組織",
                TenantName = "test-tenant"
            };

            _context.Organizations.Add(_organization);
            _context.SaveChanges();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateUser()
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                ExternalId = "admin@example.com",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = 1
            };

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotEmpty(result.User.Subject);
            Assert.Equal(request.UserType, result.User.UserType);
            Assert.Equal(request.OrganizationId, result.User.OrganizationId);
            Assert.True(result.User.CreatedAt <= DateTimeOffset.UtcNow);
            Assert.True(result.User.UpdatedAt <= DateTimeOffset.UtcNow);

            // DBに保存されていることを確認
            var saved = await _context.B2BUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Subject == result.User.Subject);
            Assert.NotNull(saved);

            // external_id は b2b_user ではなく identity 行にハッシュ化して保持される。
            var identity = await _context.B2BUserIdentities
                .IgnoreQueryFilters()
                .SingleAsync(i => i.B2BSubject == result.User.Subject);
            Assert.Equal(TestIssuerKey, identity.IssuerKey);
            Assert.Equal(ExternalIdHasher.Hash(request.ExternalId), identity.ExternalId);
        }

        [Fact]
        public async Task CreateAsync_WithoutExternalId_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                ExternalId = "",
                IssuerKey = TestIssuerKey,
                UserType = "staff",
                OrganizationId = 1
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
            Assert.Contains("ExternalId", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateUniqueSubject()
        {
            // Arrange - each request needs a unique ExternalId
            var requests = Enumerable.Range(0, 5).Select(i => new IB2BUserService.CreateUserRequest
            {
                ExternalId = $"unique-admin-{i}",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = 1
            }).ToList();

            // Act
            var subjects = new HashSet<string>();
            foreach (var request in requests)
            {
                var result = await _service.CreateAsync(request);
                Assert.True(subjects.Add(result.User.Subject), "重複したSubjectが生成されました");
            }

            // Assert
            Assert.Equal(5, subjects.Count);
        }

        [Fact]
        public async Task CreateAsync_SubjectShouldBeValidUuid()
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                ExternalId = "uuid-test-admin",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = 1
            };

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.True(Guid.TryParse(result.User.Subject, out _), "SubjectはUUID形式である必要があります");
        }

        [Fact]
        public async Task CreateAsync_WithExplicitSubject_ShouldUseProvidedSubject()
        {
            // Arrange
            var explicitSubject = "550e8400-e29b-41d4-a716-446655440099";
            var request = new IB2BUserService.CreateUserRequest
            {
                Subject = explicitSubject,
                ExternalId = "explicit-subject-admin",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = 1
            };

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(explicitSubject, result.User.Subject);
        }

        [Fact]
        public async Task CreateAsync_WithoutSubject_ShouldAutoGenerateUuid()
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                Subject = null,
                ExternalId = "auto-uuid-admin",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = 1
            };

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(Guid.TryParse(result.User.Subject, out _), "自動生成されたSubjectはUUID形式である必要があります");
        }

        [Theory]
        [InlineData("not-a-uuid")]
        [InlineData("12345")]
        [InlineData("xyz-invalid")]
        public async Task CreateAsync_InvalidUuidSubject_ShouldThrowArgumentException(string invalidSubject)
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                Subject = invalidSubject,
                ExternalId = "invalid-uuid-admin",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = 1
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
            Assert.Contains("UUID", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CreateAsync_InvalidOrganizationId_ShouldThrowArgumentException(int organizationId)
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                ExternalId = "invalid-org-admin",
                IssuerKey = TestIssuerKey,
                UserType = "admin",
                OrganizationId = organizationId
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
            Assert.Contains("OrganizationId", ex.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_EmptyUserType_ShouldThrowArgumentException(string userType)
        {
            // Arrange
            var request = new IB2BUserService.CreateUserRequest
            {
                ExternalId = "empty-usertype-admin",
                IssuerKey = TestIssuerKey,
                UserType = userType,
                OrganizationId = 1
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
            Assert.Contains("UserType", ex.Message);
        }

        #endregion

        #region GetBySubjectAsync Tests

        [Fact]
        public async Task GetBySubjectAsync_ExistingUser_ShouldReturn()
        {
            // Arrange
            var user = new B2BUser
            {
                Subject = "test-subject-123",
                UserType = "admin",
                OrganizationId = 1,
                Organization = _organization
            };
            _context.B2BUsers.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetBySubjectAsync("test-subject-123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-subject-123", result.Subject);
            Assert.Equal("admin", result.UserType);
        }

        [Fact]
        public async Task GetBySubjectAsync_NonExisting_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetBySubjectAsync("non-existing-subject");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetBySubjectAsync_InvalidSubject_ShouldReturnNull(string? subject)
        {
            // Act
            var result = await _service.GetBySubjectAsync(subject!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingUser_ShouldUpdateFields()
        {
            // Arrange
            var user = new B2BUser
            {
                Subject = "update-test-subject",
                UserType = "admin",
                OrganizationId = 1,
                Organization = _organization
            };
            _context.B2BUsers.Add(user);
            await _context.SaveChangesAsync();

            var request = new IB2BUserService.UpdateUserRequest
            {
                Subject = "update-test-subject",
                UserType = "staff"
            };

            // Act
            var result = await _service.UpdateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("staff", result.UserType);
            Assert.True(result.UpdatedAt >= user.CreatedAt);
        }

        [Fact]
        public async Task UpdateAsync_PartialUpdate_ShouldOnlyUpdateProvidedFields()
        {
            // Arrange
            var user = new B2BUser
            {
                Subject = "partial-update-subject",
                UserType = "admin",
                OrganizationId = 1,
                Organization = _organization
            };
            _context.B2BUsers.Add(user);
            await _context.SaveChangesAsync();

            var before = user.UpdatedAt;
            var request = new IB2BUserService.UpdateUserRequest
            {
                Subject = "partial-update-subject",
                UserType = null // 更新しない
            };

            // Act
            var result = await _service.UpdateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin", result.UserType); // 変更されていない
            Assert.True(result.UpdatedAt >= before);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingUser_ShouldReturnNull()
        {
            // Arrange
            var request = new IB2BUserService.UpdateUserRequest
            {
                Subject = "non-existing-subject",
                UserType = "staff"
            };

            // Act
            var result = await _service.UpdateAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateAsync_InvalidSubject_ShouldThrowArgumentException(string? subject)
        {
            // Arrange
            var request = new IB2BUserService.UpdateUserRequest
            {
                Subject = subject!,
                UserType = "staff"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateAsync(request));
            Assert.Contains("Subject", ex.Message);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingUser_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var user = new B2BUser
            {
                Subject = "delete-test-subject",
                UserType = "admin",
                OrganizationId = 1,
                Organization = _organization
            };
            _context.B2BUsers.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync("delete-test-subject");

            // Assert
            Assert.True(result);

            var deleted = await _context.B2BUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Subject == "delete-test-subject");
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingUser_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteAsync("non-existing-subject");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteAsync_InvalidSubject_ShouldReturnFalse(string? subject)
        {
            // Act
            var result = await _service.DeleteAsync(subject!);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_ExistingUser_ShouldReturnTrue()
        {
            // Arrange
            var user = new B2BUser
            {
                Subject = "exists-test-subject",
                UserType = "admin",
                OrganizationId = 1,
                Organization = _organization
            };
            _context.B2BUsers.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ExistsAsync("exists-test-subject");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_NonExistingUser_ShouldReturnFalse()
        {
            // Act
            var result = await _service.ExistsAsync("non-existing-subject");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ExistsAsync_InvalidSubject_ShouldReturnFalse(string? subject)
        {
            // Act
            var result = await _service.ExistsAsync(subject!);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CountByOrganizationAsync Tests

        [Fact]
        public async Task CountByOrganizationAsync_WithUsers_ShouldReturnCount()
        {
            // Arrange
            var users = new[]
            {
                new B2BUser { Subject = "user-1", UserType = "admin", OrganizationId = 1, Organization = _organization },
                new B2BUser { Subject = "user-2", UserType = "staff", OrganizationId = 1, Organization = _organization },
                new B2BUser { Subject = "user-3", UserType = "staff", OrganizationId = 1, Organization = _organization }
            };
            _context.B2BUsers.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CountByOrganizationAsync(1);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task CountByOrganizationAsync_NoUsers_ShouldReturnZero()
        {
            // Act
            var result = await _service.CountByOrganizationAsync(999);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CountByOrganizationAsync_ShouldOnlyCountOrganizationUsers()
        {
            // Arrange
            var org2 = new Organization
            {
                Id = 2,
                Code = "org-2",
                Name = "組織2",
                TenantName = "tenant-2"
            };
            _context.Organizations.Add(org2);

            var users = new[]
            {
                new B2BUser { Subject = "org1-user-1", UserType = "admin", OrganizationId = 1, Organization = _organization },
                new B2BUser { Subject = "org1-user-2", UserType = "staff", OrganizationId = 1, Organization = _organization },
                new B2BUser { Subject = "org2-user-1", UserType = "admin", OrganizationId = 2, Organization = org2 }
            };
            _context.B2BUsers.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var org1Count = await _service.CountByOrganizationAsync(1);
            var org2Count = await _service.CountByOrganizationAsync(2);

            // Assert
            Assert.Equal(2, org1Count);
            Assert.Equal(1, org2Count);
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
