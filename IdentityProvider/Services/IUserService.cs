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
    }
}