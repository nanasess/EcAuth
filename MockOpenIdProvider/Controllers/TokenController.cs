using IdpUtilities;
using Microsoft.AspNetCore.Mvc;
using MockOpenIdProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace MockOpenIdProvider.Controllers
{
    [Route("token")]
    public class TokenController : Controller
    {
        private readonly IdpDbContext _context;

        public TokenController(IdpDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        /// <summary>Token リクエストを受信し、アクセストークンを返します。</summary>
        /// <remarks>
        /// - Client Authentication
        /// - grant_type のチェック
        /// - authorization_code のチェック
        /// - redirect_uri のチェック
        /// </remarks>
        public async Task<IActionResult> Index([FromForm] string grant_type, [FromForm] string code, [FromForm] string redirect_uri, [FromForm] string client_id, [FromForm] string client_secret)
        {
            try
            {
                if (grant_type != "authorization_code")
                {
                    return Json(new { error = "unsupported_grant_type" });
                }
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirect_uri) || string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(client_secret))
                {
                    return Json(new { error = "invalid_request" });
                }
                if (!await _context.Clients.AnyAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret))
                {
                    return Json(new { error = "invalid_client" });
                }
                var AuthorizationCode = await _context.AuthorizationCodes.Where(
                    ac => ac.Code == code && ac.Client.ClientId == client_id && ac.Client.ClientSecret == client_secret
                ).FirstOrDefaultAsync();
                if (AuthorizationCode == null)
                {
                    return Json(new { error = "invalid_grant" });
                }
                if (AuthorizationCode.Used)
                {
                    return Json(new { error = "invalid_grant" });
                }
                if (AuthorizationCode.ExpiresIn < (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds)
                {
                    return Json(new { error = "invalid_grant" });
                }
                AuthorizationCode.Used = true;
                _context.Update(AuthorizationCode);
                var token = RandomUtil.GenerateRandomBytes(32);
                var AccessToken = new AccessToken {
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresIn = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 3600,
                    Client = AuthorizationCode.Client,
                    ClientId = AuthorizationCode.ClientId,
                    User = AuthorizationCode.User,
                    UserId = AuthorizationCode.UserId
                };
                await _context.AddAsync(AccessToken);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("logs/exceptions.log", ex.ToString());
                throw;
            }
            return Json(
                new {
                    access_token = token,
                    token_type = "Bearer",
                    expires_in = 3600,
                }
            );
        }
    }
}
