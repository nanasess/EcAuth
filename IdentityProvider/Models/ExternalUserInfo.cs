namespace IdentityProvider.Models
{
    /// <summary>
    /// 外部IdPから取得したユーザー情報を統一的に扱うためのモデル
    /// </summary>
    public class ExternalUserInfo
    {
        /// <summary>
        /// 外部IdPのユーザーID
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// メールアドレス（オプション）
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 表示名（オプション）
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// プロバイダー名（google, line等）
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// その他のクレーム
        /// </summary>
        public Dictionary<string, object>? Claims { get; set; }
    }
}