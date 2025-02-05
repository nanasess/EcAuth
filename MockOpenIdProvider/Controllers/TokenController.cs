using Microsoft.AspNetCore.Mvc;

namespace MockOpenIdProvider.Controllers
{
    [Route("token")]
    public class TokenController : Controller
    {
        [HttpPost]
        /// <summary>Token リクエストを受信し、アクセストークンを返します。</summary>
        public IActionResult Index([FromForm] string grant_type, [FromForm] string code, [FromForm] string redirect_uri, [FromForm] string client_id, [FromForm] string client_secret)
        {
            return Json(new { access_token = "accesstoken", token_type = "Bearer" });
        }
    }
}
