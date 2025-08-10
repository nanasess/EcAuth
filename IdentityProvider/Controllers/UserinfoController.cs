using IdentityProvider.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IdentityProvider.Controllers
{
    [Route("userinfo")]
    [ApiController]
    public class UserinfoController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public UserinfoController(ITokenService tokenService, IUserService userService)
        {
            _tokenService = tokenService;
            _userService = userService;
        }

        /// <summary>
        /// OpenID Connect準拠のUserInfo endpoint - アクセストークンからユーザー情報を返します。
        /// </summary>
        /// <returns>OpenID Connect準拠のユーザー情報</returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                // Authorization ヘッダーからBearerトークンを取得
                var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(new
                    {
                        error = "invalid_token",
                        error_description = "アクセストークンが提供されていません。"
                    });
                }

                var accessToken = authorizationHeader.Substring("Bearer ".Length).Trim();

                // アクセストークンを検証してユーザーのSubjectを取得
                // 注意: ここではクライアントIDが必要ですが、アクセストークンから抽出する必要があります
                // 簡易実装として、アクセストークンからSubjectを直接取得します
                var userSubject = await _tokenService.ValidateTokenAsync(accessToken, 0); // クライアントID = 0 は仮の値
                if (string.IsNullOrEmpty(userSubject))
                {
                    return Unauthorized(new
                    {
                        error = "invalid_token",
                        error_description = "無効なアクセストークンです。"
                    });
                }

                // ユーザー情報を取得
                var user = await _userService.GetUserBySubjectAsync(userSubject);
                if (user == null)
                {
                    return NotFound(new
                    {
                        error = "user_not_found",
                        error_description = "ユーザーが見つかりません。"
                    });
                }

                // OpenID Connect準拠のユーザー情報を返却
                // 注意: 個人情報保護のため、最小限の情報のみを返却
                var userInfo = new
                {
                    sub = user.Subject,
                    // email_hash = user.EmailHash, // メールハッシュは通常外部に公開しない
                    // 他の標準クレームは必要に応じて追加
                    // name, email, picture, etc.
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                // ログ出力
                System.IO.File.AppendAllText("logs/exceptions.log",
                    $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] UserInfo Error: {ex}\n");

                return StatusCode(500, new
                {
                    error = "server_error",
                    error_description = "内部サーバーエラーが発生しました。"
                });
            }
        }
    }
}