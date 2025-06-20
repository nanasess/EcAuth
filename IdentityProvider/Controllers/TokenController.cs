using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IdentityProvider.Models;
using IdentityProvider.Services;
using IdpUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace IdentityProvider.Controllers
{
    [Route("token")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly EcAuthDbContext _context;
        private readonly IHostEnvironment _environment;
        private readonly IAuthorizationCodeService _authCodeService;
        private readonly IUserService _userService;
        private readonly ILogger<TokenController> _logger;

        public TokenController(
            EcAuthDbContext context, 
            IHostEnvironment environment,
            IAuthorizationCodeService authCodeService,
            IUserService userService,
            ILogger<TokenController> logger)
        {
            _context = context;
            _environment = environment;
            _authCodeService = authCodeService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// OpenID Connect Token Endpoint
        /// Authorization Codeを検証し、IDトークンとアクセストークンを発行します。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Token(
            [FromForm] string grant_type,
            [FromForm] string code,
            [FromForm] string redirect_uri,
            [FromForm] string client_id,
            [FromForm] string client_secret)
        {
            try
            {
                // grant_typeの検証
                if (grant_type != "authorization_code")
                {
                    return BadRequest(new
                    {
                        error = "unsupported_grant_type",
                        error_description = "Only authorization_code grant type is supported"
                    });
                }

                // クライアント認証
                var client = await _context.Clients
                    .Include(c => c.Organization)
                    .Include(c => c.RsaKeyPair)
                    .FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);

                if (client == null)
                {
                    return Unauthorized(new
                    {
                        error = "invalid_client",
                        error_description = "Client authentication failed"
                    });
                }

                // 認可コードの検証
                var authCode = await _authCodeService.ValidateAuthorizationCodeAsync(code, client_id);
                if (authCode == null)
                {
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "Invalid or expired authorization code"
                    });
                }

                // redirect_uriの検証（保存されている場合）
                if (!string.IsNullOrEmpty(authCode.RedirectUri) && authCode.RedirectUri != redirect_uri)
                {
                    return BadRequest(new
                    {
                        error = "invalid_grant",
                        error_description = "Redirect URI mismatch"
                    });
                }

                // 認可コードを使用済みにマーク
                await _authCodeService.MarkCodeAsUsedAsync(authCode.Id);

                // ユーザー情報を取得
                var user = authCode.EcAuthUser;

                // IDトークンの生成
                var idToken = GenerateIdToken(user, client, authCode.Scope);

                // アクセストークンの生成（簡易実装）
                var accessToken = GenerateAccessToken(user, client, authCode.Scope);

                // レスポンスの作成
                var response = new
                {
                    access_token = accessToken,
                    token_type = "Bearer",
                    expires_in = 3600,
                    id_token = idToken,
                    scope = authCode.Scope ?? "openid"
                };

                _logger.LogInformation("Issued tokens for user {UserId} and client {ClientId}", 
                    user.Id, client.Id);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing token request");
                return StatusCode(500, new
                {
                    error = "server_error",
                    error_description = "An unexpected error occurred"
                });
            }
        }

        private string GenerateIdToken(EcAuthUser user, Client client, string? scope)
        {
            var claims = new List<Claim>
            {
                new Claim("sub", user.Subject),
                new Claim("aud", client.ClientId),
                new Claim("iss", $"https://{Request.Host}"),
                new Claim("iat", DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("exp", DateTimeOffset.Now.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // スコープに応じて追加のクレームを含める
            if (!string.IsNullOrEmpty(scope))
            {
                var scopes = scope.Split(' ');
                if (scopes.Contains("email") && !string.IsNullOrEmpty(user.EmailHash))
                {
                    // 実際の実装では、ハッシュではなく実際のメールアドレスを含める必要があります
                    // ここでは開発用にダミーのメールアドレスを設定
                    claims.Add(new Claim("email", "user@example.com"));
                    claims.Add(new Claim("email_verified", "true", ClaimValueTypes.Boolean));
                }
            }

            // RSA秘密鍵を使用してJWTに署名
            if (client.RsaKeyPair != null)
            {
                var key = new RsaSecurityKey(System.Security.Cryptography.RSA.Create());
                key.Rsa.ImportFromPem(client.RsaKeyPair.PrivateKey);

                var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
                
                var token = new JwtSecurityToken(
                    issuer: $"https://{Request.Host}",
                    audience: client.ClientId,
                    claims: claims,
                    expires: DateTimeOffset.Now.AddHours(1).DateTime,
                    signingCredentials: credentials
                );

                var handler = new JwtSecurityTokenHandler();
                return handler.WriteToken(token);
            }
            else
            {
                // 開発用の対称鍵での署名（本番環境では使用しない）
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("temporary-development-key-do-not-use-in-production"));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                
                var token = new JwtSecurityToken(
                    issuer: $"https://{Request.Host}",
                    audience: client.ClientId,
                    claims: claims,
                    expires: DateTimeOffset.Now.AddHours(1).DateTime,
                    signingCredentials: credentials
                );

                var handler = new JwtSecurityTokenHandler();
                return handler.WriteToken(token);
            }
        }

        private string GenerateAccessToken(EcAuthUser user, Client client, string? scope)
        {
            // 簡易的なアクセストークン生成（実際の実装では適切なトークン管理が必要）
            var tokenData = new
            {
                sub = user.Subject,
                client_id = client.ClientId,
                scope = scope ?? "openid",
                exp = DateTimeOffset.Now.AddHours(1).ToUnixTimeSeconds()
            };

            var tokenJson = JsonSerializer.Serialize(tokenData);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenJson));
        }
    }
}
