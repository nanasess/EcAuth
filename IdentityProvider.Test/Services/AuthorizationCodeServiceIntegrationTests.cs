using IdentityProvider.Models;
using IdentityProvider.Services;
using IdentityProvider.Test.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityProvider.Test.Services
{
    [Collection("Integration Tests")]
    public class AuthorizationCodeServiceIntegrationTests : IDisposable
    {
        private readonly EcAuthDbContext _context;
        private readonly AuthorizationCodeService _service;
        private readonly Mock<ILogger<AuthorizationCodeService>> _mockLogger;

        public AuthorizationCodeServiceIntegrationTests()
        {
            _context = TestDbContextHelper.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<AuthorizationCodeService>>();
            _service = new AuthorizationCodeService(_context, _mockLogger.Object);

            SetupTestData();
        }

        private void SetupTestData()
        {
            var organization = new Organization
            {
                Id = 1,
                Code = "integration-test-org",
                Name = "統合テスト組織",
                TenantName = "integration-test-tenant"
            };

            var client = new Client
            {
                Id = 1,
                ClientId = "integration-test-client",
                ClientSecret = "integration-test-secret",
                AppName = "Integration Test App",
                OrganizationId = 1,
                Organization = organization
            };

            var user = new EcAuthUser
            {
                Subject = "integration-test-subject",
                EmailHash = "integration-test-hash",
                OrganizationId = 1,
                Organization = organization
            };

            _context.Organizations.Add(organization);
            _context.Clients.Add(client);
            _context.EcAuthUsers.Add(user);
            _context.SaveChanges();
        }

        [Fact]
        public async Task CompleteAuthorizationCodeFlow_GenerateUseAndCleanup_WorksCorrectly()
        {
            // Arrange
            var request = new IAuthorizationCodeService.AuthorizationCodeRequest
            {
                Subject = "integration-test-subject",
                ClientId = 1,
                RedirectUri = "https://integration.example.com/callback",
                Scope = "openid profile email",
                State = "test-state-value",
                ExpirationMinutes = 1 // 1分で期限切れ
            };

            // Step 1: 認可コード生成
            var generatedCode = await _service.GenerateAuthorizationCodeAsync(request);
            Assert.NotNull(generatedCode);
            Assert.NotEmpty(generatedCode.Code);
            Assert.False(generatedCode.IsUsed);

            // Step 2: 生成されたコードを取得
            var retrievedCode = await _service.GetAuthorizationCodeAsync(generatedCode.Code);
            Assert.NotNull(retrievedCode);
            Assert.Equal(generatedCode.Code, retrievedCode.Code);
            Assert.Equal(request.Subject, retrievedCode.EcAuthSubject);
            Assert.Equal(request.ClientId, retrievedCode.ClientId);
            Assert.Equal(request.RedirectUri, retrievedCode.RedirectUri);
            Assert.Equal(request.Scope, retrievedCode.Scope);
            Assert.Equal(request.State, retrievedCode.State);

            // Step 3: コードを使用済みにマーク
            var markResult = await _service.MarkAsUsedAsync(generatedCode.Code);
            Assert.True(markResult);

            // Step 4: 使用済みコードは取得できないことを確認
            var usedCodeRetrieval = await _service.GetAuthorizationCodeAsync(generatedCode.Code);
            Assert.Null(usedCodeRetrieval);

            // Step 5: 統計情報の確認
            var stats = await _service.GetStatisticsAsync();
            Assert.Equal(1, stats.TotalCodes);
            Assert.Equal(0, stats.ActiveCodes); // 使用済みなのでアクティブではない
            Assert.Equal(1, stats.UsedCodes);

            // Step 6: 期限切れコードのクリーンアップテスト用に新しいコードを生成し、手動で期限切れにする
            var expiredCodeRequest = new IAuthorizationCodeService.AuthorizationCodeRequest
            {
                Subject = "integration-test-subject",
                ClientId = 1,
                RedirectUri = "https://integration.example.com/callback",
                ExpirationMinutes = 10
            };
            var expiredCode = await _service.GenerateAuthorizationCodeAsync(expiredCodeRequest);

            // 手動で期限切れにする
            var entityEntry = _context.Entry(expiredCode);
            entityEntry.Entity.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            await _context.SaveChangesAsync();

            // Step 7: クリーンアップの実行
            var cleanupCount = await _service.CleanupExpiredCodesAsync();
            Assert.Equal(1, cleanupCount);

            // Step 8: 最終統計の確認
            var finalStats = await _service.GetStatisticsAsync();
            Assert.Equal(1, finalStats.TotalCodes); // 使用済みのコードのみ残る
            Assert.Equal(0, finalStats.ActiveCodes);
            Assert.Equal(0, finalStats.ExpiredCodes); // クリーンアップされた
            Assert.Equal(1, finalStats.UsedCodes);
        }

        [Fact]
        public async Task ConcurrentCodeGeneration_GeneratesUniqueCodes()
        {
            // Arrange
            const int concurrentRequests = 10;
            var tasks = new List<Task<AuthorizationCode>>();

            // Act - 複数のタスクを並行実行
            for (int i = 0; i < concurrentRequests; i++)
            {
                var request = new IAuthorizationCodeService.AuthorizationCodeRequest
                {
                    Subject = $"concurrent-test-subject-{i}",
                    ClientId = 1,
                    RedirectUri = $"https://concurrent.example.com/callback/{i}",
                    Scope = "openid profile"
                };

                tasks.Add(_service.GenerateAuthorizationCodeAsync(request));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(concurrentRequests, results.Length);

            var codes = results.Select(r => r.Code).ToList();
            var uniqueCodes = codes.Distinct().ToList();

            Assert.Equal(concurrentRequests, uniqueCodes.Count); // 全てのコードが一意
        }

        [Fact]
        public async Task RateLimitingIntegration_PreventsBulkGeneration()
        {
            // Arrange
            var request = new IAuthorizationCodeService.AuthorizationCodeRequest
            {
                Subject = "rate-limit-test-subject",
                ClientId = 1,
                RedirectUri = "https://ratelimit.example.com/callback"
            };

            // Act & Assert - レート制限まで生成
            var successfulGenerations = 0;
            for (int i = 0; i < 7; i++) // レート制限は5回/分なので、6回目以降はエラー
            {
                try
                {
                    await _service.GenerateAuthorizationCodeAsync(request);
                    successfulGenerations++;
                }
                catch (InvalidOperationException ex)
                {
                    Assert.Contains("レート制限", ex.Message);
                    break;
                }
            }

            Assert.Equal(5, successfulGenerations); // レート制限により5回まで成功
        }

        [Fact]
        public async Task DatabaseTransaction_RollbackOnError()
        {
            // Arrange
            var request = new IAuthorizationCodeService.AuthorizationCodeRequest
            {
                Subject = "transaction-test-subject",
                ClientId = 1,
                RedirectUri = "https://transaction.example.com/callback"
            };

            var initialCount = await _context.AuthorizationCodes.CountAsync();

            // Act - 正常なケース
            var code1 = await _service.GenerateAuthorizationCodeAsync(request);
            var countAfterSuccess = await _context.AuthorizationCodes.CountAsync();

            // Assert
            Assert.Equal(initialCount + 1, countAfterSuccess);
            Assert.NotNull(code1);

            // 無効なリクエストでエラーが発生することを確認
            var invalidRequest = new IAuthorizationCodeService.AuthorizationCodeRequest
            {
                Subject = "", // 無効なSubject
                ClientId = 1,
                RedirectUri = "https://transaction.example.com/callback"
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GenerateAuthorizationCodeAsync(invalidRequest));

            // エラー後もデータベースの状態が正しいことを確認
            var countAfterError = await _context.AuthorizationCodes.CountAsync();
            Assert.Equal(countAfterSuccess, countAfterError); // カウントは変わらない
        }

        [Fact]
        public async Task FullWorkflowWithDatabaseConstraints_HandlesConstraintViolations()
        {
            // Arrange - 同じコードを手動で作成（通常は発生しないが、制約テストのため）
            var manualCode = new AuthorizationCode
            {
                Code = "manual-test-code",
                EcAuthSubject = "constraint-test-subject",
                ClientId = 1,
                RedirectUri = "https://constraint.example.com/callback",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.AuthorizationCodes.Add(manualCode);
            await _context.SaveChangesAsync();

            // Act & Assert - サービスは一意性を保証する
            var request = new IAuthorizationCodeService.AuthorizationCodeRequest
            {
                Subject = "constraint-test-subject",
                ClientId = 1,
                RedirectUri = "https://constraint.example.com/callback"
            };

            var generatedCode = await _service.GenerateAuthorizationCodeAsync(request);

            // 生成されたコードは手動コードと異なることを確認
            Assert.NotEqual("manual-test-code", generatedCode.Code);

            // 両方のコードがデータベースに存在することを確認
            var allCodes = await _context.AuthorizationCodes
                .Where(ac => ac.EcAuthSubject == "constraint-test-subject")
                .ToListAsync();

            Assert.Equal(2, allCodes.Count);
            Assert.Contains(allCodes, ac => ac.Code == "manual-test-code");
            Assert.Contains(allCodes, ac => ac.Code == generatedCode.Code);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}