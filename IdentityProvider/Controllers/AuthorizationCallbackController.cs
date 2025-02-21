using IdentityProvider.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web;

namespace IdentityProvider.Controllers
{
    [Route("auth/callback")]
    public class AuthorizationCallbackController : Controller
    {
        private readonly EcAuthDbContext _context;

        public AuthorizationCallbackController(EcAuthDbContext context)
        {
            _context = context;
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
    }
}
