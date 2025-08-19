using IdentityProvider.Models;

namespace IdentityProvider.Services
{
    /// <summary>
    /// 外部IdPからのユーザー情報を正規化するインターフェース
    /// </summary>
    public interface IExternalUserNormalizer
    {
        /// <summary>
        /// プロバイダー名を取得
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 外部IdPのレスポンスからExternalUserInfoに正規化
        /// </summary>
        /// <param name="externalResponse">外部IdPからのレスポンスデータ</param>
        /// <returns>正規化されたユーザー情報</returns>
        ExternalUserInfo Normalize(dynamic externalResponse);
    }
}