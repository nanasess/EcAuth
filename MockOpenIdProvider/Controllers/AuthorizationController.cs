using Microsoft.AspNetCore.Mvc;
using MockOpenIdProvider.Models;
using Microsoft.EntityFrameworkCore;
using System.Web;

namespace MockOpenIdProvider.Controllers
{
    [Route("authorization")]
    public class AuthorizationController : Controller
    {
        private readonly IdpDbContext _context;

        public AuthorizationController(IdpDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        /// <summary>Authorization リクエストを受信し、RedirectUri にリダイレクトします。</summary>
        public async Task<IActionResult> Index([FromQuery] string redirect_uri, [FromQuery] string client_id, [FromQuery] string response_type, [FromQuery] string scope, [FromQuery] string state, [FromQuery] string nonce)
        {
            var Client = await _context.Clients.Where(
                c => c.ClientId == client_id
            ).FirstOrDefaultAsync();

            if (Client?.RedirectUri == redirect_uri)
            {
                var AuthorizationCode = new AuthorizationCode {
                    Code = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresIn = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 3600,
                    Used = false,
                    Client = Client,
                    ClientId = Client.Id
                };
                await _context.AddAsync(AuthorizationCode);
                await _context.SaveChangesAsync();
                return Redirect($"{redirect_uri}?code={AuthorizationCode.Code}&state={state}&nonce={nonce}");
            }

            return Redirect(
                $"{redirect_uri}" +
                $"?error=invalid_request_uri" +
                $"&error_description={HttpUtility.UrlEncode("The request_uri in the Authorization Request returns an error or contains invalid data.")}"
            );
        }
    }
}
