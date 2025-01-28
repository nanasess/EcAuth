using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityProvider.Models;

namespace IdentityProvider.Controllers
{
    [Route("token")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly EcAuthDbContext _context;
        public TokenController(EcAuthDbContext context)
        {
            _context = context;
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
            var IdentityProvider = await _context.OpenIdProviders.Where(p => p.Id == IdentityProviderId)
                .FirstOrDefaultAsync();
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    IdentityProvider.TokenEndpoint,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", code },
                        { "redirect_uri", "https://localhost:8081/auth/callback" },//State.RedirectUri },
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
