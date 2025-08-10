using IdentityProvider.Models;
using IdentityProvider.Services;
using IdpUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace IdentityProvider.Controllers
{
    [Route("token")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly EcAuthDbContext _context;
        private readonly IHostEnvironment _environment;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public TokenController(EcAuthDbContext context, IHostEnvironment environment, ITokenService tokenService, IUserService userService)
        {
            _context = context;
            _environment = environment;
            _tokenService = tokenService;
            _userService = userService;
        }

        /// <summary>
        /// OpenID Connect準拠のToken endpoint - 認可コードをIDトークンとアクセストークンに交換します。
        /// </summary>
        /// <param name="grant_type">認可タイプ（authorization_code）</param>
        /// <param name="code">認可コード</param>
        /// <param name="redirect_uri">リダイレクトURI</param>
        /// <param name="client_id">クライアントID</param>
        /// <param name="client_secret">クライアントシークレット</param>
        /// <returns>IDトークンとアクセストークン</returns>
        [HttpPost]
        public async Task<IActionResult> Token(
            [FromForm] string grant_type,
            [FromForm] string code,
            [FromForm] string redirect_uri,
            [FromForm] string client_id,
            [FromForm] string? client_secret)
        {
            try
            {
                // grant_type の検証
                if (grant_type != "authorization_code")
                {
                    return BadRequest(new
                    {
                        error = "unsupported_grant_type",
                        error_description = "サポートされていないgrant_typeです。"
                    });
                }

                // 必須パラメータの検証
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirect_uri) || string.IsNullOrEmpty(client_id))
                {
                    return BadRequest(new
                    {
                        error = "invalid_request",
                        error_description = "必須パラメータが不足しています。"
                    });
                }

                // client_idの変換
                if (!int.TryParse(client_id, out int clientIdInt))
                {
                    return BadRequest(new
                    {
                        error = "invalid_client",
                        error_description = "無効なクライアントIDです。"
                    });
                }

                // クライアントの検証
                var client = await _context.Clients
                    .Include(c => c.Organization)
                    .FirstOrDefaultAsync(c => c.Id == clientIdInt);

                if (client == null)
                {
                    return BadRequest(new
                    {
                        error = "invalid_client",
                        error_description = "クライアントが見つかりません。"
                    });
                }

                // クライアントシークレットの検証（設定されている場合）
                if (!string.IsNullOrEmpty(client.ClientSecret) && client.ClientSecret != client_secret)
                {
                    return Unauthorized(new
                    {
                        error = "invalid_client",
                        error_description = "クライアント認証に失敗しました。"
                    });
                }

                // 認可コードの検証
                var authCode = await _context.AuthorizationCodes
                    .Include(ac => ac.EcAuthUser)
                    .Include(ac => ac.Client)
                    .FirstOrDefaultAsync(ac => ac.Code == code && ac.ClientId == clientIdInt);

                if (authCode == null)
                {
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "無効な認可コードです。"
                    });
                }

                // 認可コードの有効期限チェック
                if (authCode.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "認可コードの有効期限が切れています。"
                    });
                }

                // 認可コードの使用済みチェック
                if (authCode.IsUsed)
                {
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "認可コードは既に使用されています。"
                    });
                }

                // redirect_uriの検証
                if (authCode.RedirectUri != redirect_uri)
                {
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "リダイレクトURIが一致しません。"
                    });
                }

                // ユーザー情報の取得
                var user = await _userService.GetUserBySubjectAsync(authCode.EcAuthSubject);
                if (user == null)
                {
                    return StatusCode(500, new
                    {
                        error = "server_error",
                        error_description = "ユーザー情報が見つかりません。"
                    });
                }

                // トークン生成
                var tokenRequest = new ITokenService.TokenRequest
                {
                    User = user,
                    Client = client,
                    RequestedScopes = authCode.Scope?.Split(' '),
                    Nonce = null // 必要に応じてNonceを実装
                };

                var tokenResponse = await _tokenService.GenerateTokensAsync(tokenRequest);

                // 認可コードを使用済みにマーク
                authCode.IsUsed = true;
                authCode.UsedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                // OpenID Connect準拠のレスポンス
                return Ok(new
                {
                    access_token = tokenResponse.AccessToken,
                    token_type = tokenResponse.TokenType,
                    expires_in = tokenResponse.ExpiresIn,
                    id_token = tokenResponse.IdToken,
                    refresh_token = tokenResponse.RefreshToken
                });
            }
            catch (Exception ex)
            {
                // ログ出力
                System.IO.File.AppendAllText("logs/exceptions.log",
                    $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Token Error: {ex}\n");

                return StatusCode(500, new
                {
                    error = "server_error",
                    error_description = "内部サーバーエラーが発生しました。"
                });
            }
        }

        /// <summary>
        /// 外部IdP の Token endpoint にリクエストを送信し、アクセストークンを取得します。
        /// 取得したアクセストークンを返却します。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        [HttpPost("exchange")]
        public async Task<IActionResult> Exchange([FromForm] string code, [FromForm] string state, [FromForm] string scope)
        {
            var password = Environment.GetEnvironmentVariable("STATE_PASSWORD");
            var options = new Iron.Options();
            var State = await Iron.Unseal<State>(state, password, options);
            var IdentityProviderId = State.OpenIdProviderId;
            var IdentityProvider = await _context.OpenIdProviders
                .Where(p => p.Id == IdentityProviderId)
                .FirstOrDefaultAsync();

            var handler = _environment.IsDevelopment()
                ? new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback
                        = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }
                : new HttpClientHandler();

            using (var client = new HttpClient(handler))
            {
                var response = await client.PostAsync(
                    IdentityProvider.TokenEndpoint,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", code },
                        { "redirect_uri", "https://localhost:8081/auth/callback" },
                        { "client_id", IdentityProvider.IdpClientId },
                        { "client_secret", IdentityProvider.IdpClientSecret },
                        { "state", state }
                    })
                );

                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }
        }
    }
}
