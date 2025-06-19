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
        private readonly ILogger<AuthorizationCallbackController> _logger;

        public AuthorizationCallbackController(
            EcAuthDbContext context, 
            IUserService userService,
            ILogger<AuthorizationCallbackController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
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
                // Stateをアンシールして情報を取得
                var password = Environment.GetEnvironmentVariable("STATE_PASSWORD");
                var options = new Iron.Options();
                var stateData = await Iron.Unseal<State>(state, password, options);
                
                // OpenIdProviderの情報を取得
                var openIdProvider = await _context.OpenIdProviders
                    .Include(p => p.Client)
                        .ThenInclude(c => c.Organization)
                    .Where(p => p.Id == stateData.OpenIdProviderId)
                    .FirstOrDefaultAsync();

                if (openIdProvider == null)
                {
                    _logger.LogError("OpenIdProvider not found for ID: {ProviderId}", stateData.OpenIdProviderId);
                    return BadRequest("Invalid state");
                }

                // 外部IdPからユーザー情報を取得（現在はモック実装）
                var externalIdpService = HttpContext.RequestServices.GetRequiredService<IExternalIdpService>();
                var externalUserInfo = await externalIdpService.GetExternalUserInfoAsync(
                    openIdProvider.Name,
                    code,
                    openIdProvider.IdpClientId,
                    openIdProvider.IdpClientSecret
                );

                if (externalUserInfo == null)
                {
                    _logger.LogError("Failed to get user info from external IdP: {Provider}", openIdProvider.Name);
                    return Redirect($"{stateData.RedirectUri}?error=access_denied&error_description=Failed+to+authenticate+with+external+provider");
                }

                // OrganizationIdの検証
                if (!openIdProvider.Client.OrganizationId.HasValue)
                {
                    _logger.LogError("Client {ClientId} does not have an OrganizationId", openIdProvider.ClientId);
                    return BadRequest("Client configuration error");
                }

                // EcAuthユーザーを取得または作成（JITプロビジョニング）
                var userCreationRequest = new UserCreationRequest
                {
                    ExternalProvider = openIdProvider.Name,
                    ExternalSubject = externalUserInfo.Subject,
                    Email = externalUserInfo.Email,
                    OrganizationId = openIdProvider.Client.OrganizationId.Value
                };

                var ecAuthUser = await _userService.GetOrCreateUserAsync(userCreationRequest);

                // EcAuth用の認可コードを生成
                var authCodeService = HttpContext.RequestServices.GetRequiredService<IAuthorizationCodeService>();
                var authorizationCode = await authCodeService.GenerateAuthorizationCodeAsync(
                    openIdProvider.Client.Id,
                    ecAuthUser.Id,
                    stateData.RedirectUri,
                    scope
                );

                _logger.LogInformation("Generated authorization code for user {UserId} and client {ClientId}", 
                    ecAuthUser.Id, openIdProvider.ClientId);

                // クライアントにリダイレクト（EcAuthの認可コードを付与）
                return Redirect(
                    $"{stateData.RedirectUri}" +
                    $"?code={authorizationCode}" +
                    $"&state={HttpUtility.UrlEncode(state)}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing authorization callback");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
