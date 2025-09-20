using IdentityProvider.Models;
using IdentityProvider.Services;
using IdpUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web;
using System.Text.Json;

namespace IdentityProvider.Controllers
{
    [Route("auth/callback")]
    public class AuthorizationCallbackController : Controller
    {
        private readonly EcAuthDbContext _context;
        private readonly IAuthorizationCodeService _authorizationCodeService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthorizationCallbackController> _logger;
        private readonly IHostEnvironment _environment;

        public AuthorizationCallbackController(
            EcAuthDbContext context,
            IAuthorizationCodeService authorizationCodeService,
            IUserService userService,
            ILogger<AuthorizationCallbackController> logger,
            IHostEnvironment environment)
        {
            _context = context;
            _authorizationCodeService = authorizationCodeService;
            _userService = userService;
            _logger = logger;
            _environment = environment;
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
            try
            {
                _logger.LogInformation("Authorization callback received with code: {Code}", code);

                // 1. State パラメータをデコード
                var password = Environment.GetEnvironmentVariable("STATE_PASSWORD");
                var options = new Iron.Options();
                var stateData = await Iron.Unseal<State>(state, password, options);

                // 2. OpenID Provider 情報を取得
                var identityProvider = await _context.OpenIdProviders
                    .Where(p => p.Id == stateData.OpenIdProviderId)
                    .FirstOrDefaultAsync();

                if (identityProvider == null)
                {
                    _logger.LogError("Identity provider not found: {ProviderId}", stateData.OpenIdProviderId);
                    return BadRequest("Identity provider not found");
                }

                // 3. 外部IdPのトークンエンドポイントを呼び出してアクセストークンを取得
                var accessToken = await ExchangeCodeForTokenAsync(identityProvider, code);
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Failed to exchange code for access token");
                    return BadRequest("Failed to exchange authorization code");
                }

                // 4. 外部IdPのユーザー情報を取得
                var externalUserInfo = await GetExternalUserInfoAsync(identityProvider, accessToken);
                if (externalUserInfo == null)
                {
                    _logger.LogError("Failed to get user info from external provider");
                    return BadRequest("Failed to get user information");
                }

                // 5. JITプロビジョニング：ユーザーを取得または作成
                var ecAuthUser = await _userService.CreateOrUpdateFromExternalAsync(
                    externalUserInfo, stateData.OrganizationId);

                // 6. EcAuth独自の認可コードを生成
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == stateData.ClientId);

                if (client == null)
                {
                    _logger.LogError("Client not found: {ClientId}", stateData.ClientId);
                    return BadRequest("Client not found");
                }

                var authCodeRequest = new IAuthorizationCodeService.AuthorizationCodeRequest
                {
                    Subject = ecAuthUser.Subject,
                    ClientId = client.Id,
                    RedirectUri = stateData.RedirectUri,
                    Scope = stateData.Scope,
                    State = state,
                    ExpirationMinutes = 10
                };

                var authorizationCode = await _authorizationCodeService.GenerateAuthorizationCodeAsync(authCodeRequest);

                _logger.LogInformation("Generated EcAuth authorization code for user: {Subject}", ecAuthUser.Subject);

                // 7. クライアントにリダイレクト（EcAuth独自の認可コードを使用）
                return Redirect(
                    $"{stateData.RedirectUri}" +
                    $"?code={authorizationCode.Code}" +
                    $"&scope={stateData.Scope}" +
                    $"&state={HttpUtility.UrlEncode(state)}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authorization callback");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// 外部IdPの認可コードをアクセストークンに交換
        /// </summary>
        private async Task<string?> ExchangeCodeForTokenAsync(OpenIdProvider provider, string code)
        {
            try
            {
                var handler = _environment.IsDevelopment()
                    ? new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback
                            = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }
                    : new HttpClientHandler();

                using var client = new HttpClient(handler);

                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", "https://localhost:8081/auth/callback" },
                    { "client_id", provider.IdpClientId },
                    { "client_secret", provider.IdpClientSecret }
                });

                var response = await client.PostAsync(provider.TokenEndpoint, tokenRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Token exchange failed: {StatusCode}, {Content}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var tokenContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(tokenContent);

                return tokenResponse?.ContainsKey("access_token") == true
                    ? tokenResponse["access_token"].ToString()
                    : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during token exchange");
                return null;
            }
        }

        /// <summary>
        /// 外部IdPからユーザー情報を取得
        /// </summary>
        private async Task<ExternalUserInfo?> GetExternalUserInfoAsync(OpenIdProvider provider, string accessToken)
        {
            try
            {
                var handler = _environment.IsDevelopment()
                    ? new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback
                            = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }
                    : new HttpClientHandler();

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync(provider.UserinfoEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("UserInfo request failed: {StatusCode}, {Content}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var userInfoContent = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(userInfoContent);

                if (userInfo == null)
                {
                    _logger.LogError("Failed to parse user info response");
                    return null;
                }

                return new ExternalUserInfo
                {
                    Provider = provider.Name,
                    Subject = userInfo.ContainsKey("sub") ? userInfo["sub"].ToString() ?? "" : "",
                    Email = userInfo.ContainsKey("email") ? userInfo["email"].ToString() ?? "" : ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during user info retrieval");
                return null;
            }
        }
    }
}
