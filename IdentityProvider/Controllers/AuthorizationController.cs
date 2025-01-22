using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityProvider;
using IdentityProvider.Models;

namespace IdentityProvider.Controllers
{
    [Route("authorization")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        private readonly EcAuthDbContext _context;

        public AuthorizationController(EcAuthDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        /// <summary>IdP の Authorization endpoint にリダイレクトします。</summary>
        /// <remarks>
        /// 必要なパラメータは以下のとおり
        /// - client.client_id
        /// - open_id_provider.name
        /// パラメータで OpenID Provider を特定し、その IdP の Authorization endpoint にリダイレクトします。
        /// リダイレクト時に以下のパラメータを付与します。
        /// - client_id=<open_id_provider.client_id>
        /// - scope=<open_id_provider_scope.scope(スペース区切り)>
        /// - response_type=code
        /// - redirect_uri=<clientに登録したredirect_uri>
        /// - state=<state>
        /// </remarks>
        public async Task<IActionResult> Federate()
        {
            var Client = await _context.Clients.FirstOrDefaultAsync();
            return Redirect("/authorization/clients");
        }
    }
}
