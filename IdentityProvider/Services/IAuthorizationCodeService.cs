using IdentityProvider.Models;

namespace IdentityProvider.Services
{
    public interface IAuthorizationCodeService
    {
        /// <summary>
        /// 認可コードを生成する
        /// </summary>
        Task<string> GenerateAuthorizationCodeAsync(
            int clientId, 
            int ecAuthUserId, 
            string? redirectUri = null, 
            string? scope = null);

        /// <summary>
        /// 認可コードを検証し、取得する
        /// </summary>
        Task<AuthorizationCode?> ValidateAuthorizationCodeAsync(string code, string clientId);

        /// <summary>
        /// 認可コードを使用済みにする
        /// </summary>
        Task MarkCodeAsUsedAsync(int codeId);
    }
}