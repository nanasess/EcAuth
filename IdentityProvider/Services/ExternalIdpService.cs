using System.Net.Http.Headers;
using System.Text.Json;

namespace IdentityProvider.Services
{
    public class ExternalIdpService : IExternalIdpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExternalIdpService> _logger;

        public ExternalIdpService(IHttpClientFactory httpClientFactory, ILogger<ExternalIdpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ExternalUserInfo?> GetExternalUserInfoAsync(
            string provider, 
            string authorizationCode, 
            string idpClientId, 
            string idpClientSecret)
        {
            // 開発環境用のモック実装
            // 実際の実装では、各プロバイダーのトークンエンドポイントとユーザー情報エンドポイントを呼び出す
            if (provider.ToLower() == "mock" || provider.ToLower() == "mockoidprovider")
            {
                // MockOpenIdProviderの場合、簡単なモックデータを返す
                return new ExternalUserInfo
                {
                    Subject = $"mock-user-{Guid.NewGuid():N}",
                    Email = "test@example.com",
                    Name = "Test User"
                };
            }

            // TODO: 実際の外部IdP実装
            // 1. トークンエンドポイントを呼び出してアクセストークンを取得
            // 2. ユーザー情報エンドポイントを呼び出してユーザー情報を取得
            
            _logger.LogWarning("External IdP {Provider} is not implemented yet", provider);
            return null;
        }
    }
}