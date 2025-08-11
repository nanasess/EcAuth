using IdentityProvider.Models;
using IdentityProvider.Services;
using IdpUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

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
        private readonly ILogger<TokenController> _logger;

        public TokenController(
            EcAuthDbContext context, 
            IHostEnvironment environment,
            ITokenService tokenService,
            IUserService userService,
            ILogger<TokenController> logger)
        {
            _context = context;
            _environment = environment;
            _tokenService = tokenService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// IdP の Token endpoint にリクエストを送信し、アクセストークンを取得します。
        /// 取得したアクセストークンを返却します。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        [HttpPost]
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

        /// <summary>
        /// OpenID Connect準拠のToken endpoint
        /// 認可コードをIDトークンとアクセストークンに交換します
        /// </summary>
        /// <param name="grant_type">グラントタイプ（"authorization_code"のみサポート）</param>
        /// <param name="code">認可コード</param>
        /// <param name="redirect_uri">リダイレクトURI</param>
        /// <param name="client_id">クライアントID</param>
        /// <param name="client_secret">クライアントシークレット</param>
        /// <returns>トークンレスポンス</returns>
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Token(
            [FromForm, Required] string grant_type,
            [FromForm, Required] string code,
            [FromForm, Required] string redirect_uri,
            [FromForm, Required] string client_id,
            [FromForm] string? client_secret)
        {
            try
            {
                _logger.LogInformation("Token endpoint accessed with grant_type: {GrantType}, client_id: {ClientId}", grant_type, client_id);

                // 1. grant_typeの検証
                if (grant_type != "authorization_code")
                {
                    _logger.LogWarning("Unsupported grant_type: {GrantType}", grant_type);
                    return BadRequest(new
                    {
                        error = "unsupported_grant_type",
                        error_description = "grant_typeはauthorization_codeのみサポートしています。"
                    });
                }

                // 2. client_idの数値変換
                if (!int.TryParse(client_id, out int clientIdInt))
                {
                    _logger.LogWarning("Invalid client_id format: {ClientId}", client_id);
                    return BadRequest(new
                    {
                        error = "invalid_client",
                        error_description = "client_idの形式が正しくありません。"
                    });
                }

                // 3. クライアントの存在確認
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == clientIdInt);

                if (client == null)
                {
                    _logger.LogWarning("Client not found: {ClientId}", clientIdInt);
                    return BadRequest(new
                    {
                        error = "invalid_client",
                        error_description = "クライアントが見つかりません。"
                    });
                }

                // 4. client_secretの検証（設定されている場合のみ）
                if (!string.IsNullOrEmpty(client.ClientSecret))
                {
                    if (string.IsNullOrEmpty(client_secret) || client.ClientSecret != client_secret)
                    {
                        _logger.LogWarning("Invalid client_secret for client: {ClientId}", clientIdInt);
                        return BadRequest(new
                        {
                            error = "invalid_client",
                            error_description = "client_secretが正しくありません。"
                        });
                    }
                }

                // 5. 認可コードの取得・検証
                var authorizationCode = await _context.AuthorizationCodes
                    .Include(ac => ac.EcAuthUser)
                    .FirstOrDefaultAsync(ac => ac.Code == code);

                if (authorizationCode == null)
                {
                    _logger.LogWarning("Authorization code not found: {Code}", code);
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "認可コードが見つかりません。"
                    });
                }

                // 6. 認可コードの有効期限チェック
                if (authorizationCode.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Authorization code expired: {Code}, ExpiresAt: {ExpiresAt}", code, authorizationCode.ExpiresAt);
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "認可コードの有効期限が切れています。"
                    });
                }

                // 7. 認可コードの使用済み状態チェック
                if (authorizationCode.IsUsed)
                {
                    _logger.LogWarning("Authorization code already used: {Code}, UsedAt: {UsedAt}", code, authorizationCode.UsedAt);
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "認可コードは既に使用されています。"
                    });
                }

                // 8. redirect_uriの一致確認
                if (authorizationCode.RedirectUri != redirect_uri)
                {
                    _logger.LogWarning("Redirect URI mismatch. Expected: {Expected}, Provided: {Provided}", 
                        authorizationCode.RedirectUri, redirect_uri);
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "redirect_uriが一致しません。"
                    });
                }

                // 9. クライアントIDの一致確認
                if (authorizationCode.ClientId != clientIdInt)
                {
                    _logger.LogWarning("Client ID mismatch. Expected: {Expected}, Provided: {Provided}", 
                        authorizationCode.ClientId, clientIdInt);
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "client_idが一致しません。"
                    });
                }

                // 10. 認可コードを使用済みにマーキング
                authorizationCode.IsUsed = true;
                authorizationCode.UsedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                // 11. ユーザー情報の取得
                var user = await _userService.GetUserBySubjectAsync(authorizationCode.EcAuthSubject);
                if (user == null)
                {
                    _logger.LogError("User not found for subject: {Subject}", authorizationCode.EcAuthSubject);
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "ユーザーが見つかりません。"
                    });
                }

                // 12. トークン生成リクエストの構築
                var scopes = string.IsNullOrEmpty(authorizationCode.Scope) 
                    ? null 
                    : authorizationCode.Scope.Split(' ');

                var tokenRequest = new ITokenService.TokenRequest
                {
                    User = user,
                    Client = client,
                    RequestedScopes = scopes
                };

                // 13. トークンの生成
                var tokenResponse = await _tokenService.GenerateTokensAsync(tokenRequest);

                _logger.LogInformation("Tokens generated successfully for user: {Subject}, client: {ClientId}", 
                    user.Subject, clientIdInt);

                // 14. OpenID Connect準拠のレスポンス
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
                _logger.LogError(ex, "Error occurred in Token endpoint");
                return StatusCode(500, new
                {
                    error = "server_error",
                    error_description = "サーバー内部エラーが発生しました。"
                });
            }
        }
    }
}
