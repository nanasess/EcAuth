namespace IdentityProvider.Services
{
    public interface IExternalIdpService
    {
        /// <summary>
        /// 外部IdPから認可コードを使ってユーザー情報を取得する
        /// </summary>
        Task<ExternalUserInfo?> GetExternalUserInfoAsync(
            string provider, 
            string authorizationCode, 
            string idpClientId, 
            string idpClientSecret);
    }

    public class ExternalUserInfo
    {
        public string Subject { get; set; } = null!;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public Dictionary<string, object> AdditionalClaims { get; set; } = new();
    }
}