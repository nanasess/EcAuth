using IdentityProvider.Models;
using IdentityProvider.Services;
using IdpUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web;

namespace IdentityProvider.Controllers
{
    [Route("auth/callback")]
    public class AuthorizationCallbackController : Controller
    {
        private readonly EcAuthDbContext _context;
        private readonly IUserService _userService;

        public AuthorizationCallbackController(EcAuthDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [HttpGet]
        /// <summary>IdP の Authorization endpoint からのコールバックを受け取ります。</summary>
        /// <remarks>
        /// IdP の Authorization endpoint からのコールバックを受け取り、認可画面を表示します。
        /// 認可された場合のパラメータは以下のとおり
        /// - code=<authorization_code>
        /// - state=<state>
        /// - scope=<scope>
        /// 認可されなかった場合のパラメータは以下のとおり
        /// - error=<error>
        /// - error_description=<error_description>
        /// - state=<state>
        /// </remarks>
        public async Task<IActionResult> Index([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? scope, [FromQuery] string? error, [FromQuery] string? error_description)
        {
            try
            {
                var ecAuthDbContext = _context.Clients.Include(c => c.Organization);
                ViewData["Code"] = code;
                ViewData["State"] = state;
                ViewData["Scope"] = scope;
                return View("Index");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("logs/exceptions.log", ex.ToString());
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromForm] string code, [FromForm] string state, [FromForm] string scope)
        {
            var password = Environment.GetEnvironmentVariable("STATE_PASSWORD");
            var options = new Iron.Options();
            var State = await Iron.Unseal<State>(state, password, options);
            var IdentityProviderId = State.OpenIdProviderId;
            var IdentityProvider = await _context.OpenIdProviders
                .Where(p => p.Id == IdentityProviderId)
                .FirstOrDefaultAsync();
            return Redirect(
                $"{State.RedirectUri}" +
                $"?code={code}" +
                $"&scope={scope}" +
                $"&response_type=code" +
                $"&redirect_uri={HttpUtility.UrlEncode("https://localhost:8081/auth/callback")}" +
                $"&state={HttpUtility.UrlEncode(state)}"
             );
        }

        [HttpGet("{provider}")]
        /// <summary>外部IdPからのコールバックを受け取り、JITプロビジョニングを実行します。</summary>
        /// <param name="provider">外部IdPプロバイダー名（google、line等）</param>
        /// <param name="code">認可コード</param>
        /// <param name="state">暗号化されたStateパラメータ</param>
        /// <param name="error">エラーコード（認可失敗時）</param>
        /// <param name="error_description">エラー説明（認可失敗時）</param>
        public async Task<IActionResult> ExternalCallback(
            string provider,
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            [FromQuery] string? error_description)
        {
            try
            {
                // エラーチェック
                if (!string.IsNullOrEmpty(error))
                {
                    return BadRequest($"外部IdP認証エラー: {error} - {error_description}");
                }

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    return BadRequest("必須パラメータ（code、state）が不足しています。");
                }

                // Stateパラメータを復号化
                var password = Environment.GetEnvironmentVariable("STATE_PASSWORD");
                if (string.IsNullOrEmpty(password))
                {
                    return StatusCode(500, "STATE_PASSWORD環境変数が設定されていません。");
                }

                var options = new Iron.Options();
                var stateData = await Iron.Unseal<State>(state, password, options);

                // 外部IdPから実際のユーザー情報を取得（モック実装）
                var externalUserInfo = await GetExternalUserInfoAsync(provider, code, stateData);
                if (externalUserInfo == null)
                {
                    return StatusCode(500, "外部IdPからユーザー情報を取得できませんでした。");
                }

                // JITプロビジョニング: ユーザーを取得または作成
                var userCreationRequest = new IUserService.UserCreationRequest
                {
                    ExternalProvider = provider,
                    ExternalSubject = externalUserInfo.Subject,
                    EmailHash = EmailHashUtil.HashEmail(externalUserInfo.Email),
                    OrganizationId = stateData.OrganizationId
                };

                var ecAuthUser = await _userService.GetOrCreateUserAsync(userCreationRequest);

                // 認可コードを生成
                var authorizationCode = await GenerateAuthorizationCodeAsync(
                    ecAuthUser.Subject,
                    stateData.ClientId,
                    stateData.RedirectUri,
                    stateData.Scope,
                    state);

                // クライアントにリダイレクト
                var redirectUrl = $"{stateData.RedirectUri}?code={authorizationCode}&state={HttpUtility.UrlEncode(state)}";
                if (!string.IsNullOrEmpty(stateData.Scope))
                {
                    redirectUrl += $"&scope={HttpUtility.UrlEncode(stateData.Scope)}";
                }

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                // ログ出力
                System.IO.File.AppendAllText("logs/exceptions.log", 
                    $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ExternalCallback Error: {ex}\n");
                return StatusCode(500, "内部サーバーエラーが発生しました。");
            }
        }

        /// <summary>
        /// 外部IdPからユーザー情報を取得します（モック実装）
        /// </summary>
        private async Task<ExternalUserInfo?> GetExternalUserInfoAsync(string provider, string code, State stateData)
        {
            // 実際の実装では、外部IdPのAPIを呼び出してユーザー情報を取得します
            // ここではモック実装として固定値を返します
            await Task.Delay(1); // 非同期処理のモック

            switch (provider.ToLower())
            {
                case "google":
                    return new ExternalUserInfo
                    {
                        Subject = $"google_user_{Guid.NewGuid()}",
                        Email = "test@example.com",
                        Name = "Google Test User"
                    };
                case "line":
                    return new ExternalUserInfo
                    {
                        Subject = $"line_user_{Guid.NewGuid()}",
                        Email = "lineuser@example.com",
                        Name = "LINE Test User"
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// 認可コードを生成してデータベースに保存します
        /// </summary>
        private async Task<string> GenerateAuthorizationCodeAsync(
            string ecAuthSubject,
            int clientId,
            string redirectUri,
            string? scope,
            string state)
        {
            var code = Guid.NewGuid().ToString("N");
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10); // 10分後に有効期限切れ

            var authorizationCode = new AuthorizationCode
            {
                Code = code,
                EcAuthSubject = ecAuthSubject,
                ClientId = clientId,
                RedirectUri = redirectUri,
                Scope = scope,
                State = state,
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.AuthorizationCodes.Add(authorizationCode);
            await _context.SaveChangesAsync();

            return code;
        }

        /// <summary>
        /// 外部IdPから取得したユーザー情報
        /// </summary>
        private class ExternalUserInfo
        {
            public string Subject { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
