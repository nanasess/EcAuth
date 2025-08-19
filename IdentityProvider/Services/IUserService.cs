using IdentityProvider.Models;

namespace IdentityProvider.Services
{
    public interface IUserService
    {
        /// <summary>
        /// ユーザー作成リクエストデータ
        /// </summary>
        public class UserCreationRequest
        {
            public string ExternalProvider { get; set; } = string.Empty;
            public string ExternalSubject { get; set; } = string.Empty;
            public string EmailHash { get; set; } = string.Empty;
            public int OrganizationId { get; set; }
        }

        /// <summary>
        /// JITプロビジョニング: ユーザーを取得または作成する
        /// </summary>
        /// <param name="request">ユーザー作成リクエスト</param>
        /// <returns>EcAuthUser</returns>
        Task<EcAuthUser> GetOrCreateUserAsync(UserCreationRequest request);

        /// <summary>
        /// Subjectでユーザーを取得する
        /// </summary>
        /// <param name="subject">ユーザーのSubject</param>
        /// <returns>EcAuthUser または null</returns>
        Task<EcAuthUser?> GetUserBySubjectAsync(string subject);

        /// <summary>
        /// 外部IdP情報でユーザーを取得する
        /// </summary>
        /// <param name="externalProvider">外部IdPプロバイダー名</param>
        /// <param name="externalSubject">外部IdPのSubject</param>
        /// <param name="organizationId">組織ID</param>
        /// <returns>EcAuthUser または null</returns>
        Task<EcAuthUser?> GetUserByExternalIdAsync(string externalProvider, string externalSubject, int organizationId);

        /// <summary>
        /// 外部IdPユーザー情報からユーザーを作成または更新する
        /// </summary>
        /// <param name="externalUser">外部IdPユーザー情報</param>
        /// <param name="organizationId">組織ID</param>
        /// <returns>EcAuthUser</returns>
        Task<EcAuthUser> CreateOrUpdateFromExternalAsync(ExternalUserInfo externalUser, int organizationId);

        /// <summary>
        /// メールハッシュでユーザーを検索する（複数組織対応）
        /// </summary>
        /// <param name="emailHash">メールアドレスのハッシュ</param>
        /// <param name="organizationId">組織ID（nullの場合は全組織）</param>
        /// <returns>ユーザーリスト</returns>
        Task<List<EcAuthUser>> GetUsersByEmailHashAsync(string emailHash, int? organizationId = null);
    }
}