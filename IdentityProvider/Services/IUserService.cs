using IdentityProvider.Models;

namespace IdentityProvider.Services
{
    public interface IUserService
    {
        /// <summary>
        /// 外部IdPから取得した情報を基にEcAuthユーザーを取得または作成する（JITプロビジョニング）
        /// </summary>
        Task<EcAuthUser> GetOrCreateUserAsync(UserCreationRequest request);

        /// <summary>
        /// Subjectからユーザーを取得する
        /// </summary>
        Task<EcAuthUser?> GetUserBySubjectAsync(string subject);

        /// <summary>
        /// 外部IdPのsubjectからユーザーを取得する
        /// </summary>
        Task<EcAuthUser?> GetUserByExternalProviderAsync(string provider, string externalSubject);

        /// <summary>
        /// メールアドレスをハッシュ化する
        /// </summary>
        string HashEmail(string email);
    }

    public class UserCreationRequest
    {
        public string ExternalProvider { get; set; } = null!;
        public string ExternalSubject { get; set; } = null!;
        public string? Email { get; set; }
        public int OrganizationId { get; set; }
    }
}