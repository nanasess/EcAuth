using IdentityProvider.Models;
using IdpUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityProvider.Services
{
    public class TokenService : ITokenService
    {
        private readonly EcAuthDbContext _context;
        private readonly ILogger<TokenService> _logger;

        public TokenService(EcAuthDbContext context, ILogger<TokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ITokenService.TokenResponse> GenerateTokensAsync(ITokenService.TokenRequest request)
        {
            var idToken = await GenerateIdTokenAsync(request);
            var accessToken = await GenerateAccessTokenAsync(request);

            return new ITokenService.TokenResponse
            {
                IdToken = idToken,
                AccessToken = accessToken,
                ExpiresIn = 3600, // 1時間
                TokenType = "Bearer"
            };
        }

        public async Task<string> GenerateIdTokenAsync(ITokenService.TokenRequest request)
        {
            if (request.User == null)
                throw new ArgumentException("User cannot be null.", nameof(request.User));
            if (request.Client == null)
                throw new ArgumentException("Client cannot be null.", nameof(request.Client));

            // RSA鍵ペアを取得
            var rsaKeyPair = await _context.RsaKeyPairs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.ClientId == request.Client.Id);

            if (rsaKeyPair == null)
                throw new InvalidOperationException($"RSA key pair not found for client {request.Client.Id}");

            using (var rsa = RSA.Create())
            {
                try
                {
                    rsa.ImportRSAPrivateKey(Convert.FromBase64String(rsaKeyPair.PrivateKey), out _);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to import RSA private key for client {request.Client.Id}: {ex.Message}", ex);
                }

                var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
                {
                    CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                };

                var now = DateTime.UtcNow;
                var expires = now.AddHours(1);

                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, request.User.Subject),
                    new(JwtRegisteredClaimNames.Iss, GetIssuer()),
                    new(JwtRegisteredClaimNames.Aud, request.Client.ClientId),
                    new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // nonceが指定されている場合は追加
                if (!string.IsNullOrEmpty(request.Nonce))
                {
                    claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, request.Nonce));
                }

                // 追加のクレーム（スコープに基づいて）
                if (request.RequestedScopes?.Contains("email") == true)
                {
                    // メールアドレスはハッシュ化されているため、実際のメールアドレスは返さない
                    // 必要に応じて外部IdPから取得した情報を含める実装を検討
                    claims.Add(new Claim("email_verified", "true"));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    NotBefore = now,
                    IssuedAt = now,
                    SigningCredentials = signingCredentials
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);
                

                return tokenString;
            }
        }

        public async Task<string> GenerateAccessTokenAsync(ITokenService.TokenRequest request)
        {
            if (request.User == null)
                throw new ArgumentException("User cannot be null.", nameof(request.User));
            if (request.Client == null)
                throw new ArgumentException("Client cannot be null.", nameof(request.Client));

            // アクセストークンは簡単なランダム文字列として生成
            // より複雑な要件がある場合はJWTベースにも変更可能
            var accessToken = RandomUtil.GenerateRandomBytes(32);

            // 必要に応じて、アクセストークンをデータベースに保存
            // （リボケーションや詳細な管理が必要な場合）

            return accessToken;
        }

        public async Task<string?> ValidateTokenAsync(string token, int clientId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // クライアントのRSA公開鍵を取得
                var rsaKeyPair = await _context.RsaKeyPairs
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(k => k.ClientId == clientId);

                if (rsaKeyPair == null)
                {
                    _logger.LogWarning("RSA key pair not found for client {ClientId}", clientId);
                    return null;
                }

                using (var rsa = RSA.Create())
                {
                    try
                    {
                        // 検証では公開鍵を使用
                        rsa.ImportRSAPublicKey(Convert.FromBase64String(rsaKeyPair.PublicKey), out _);
                    }
                    catch (Exception ex)
                    {
                        // 公開鍵のインポートに失敗した場合、プライベートキーから公開鍵を取得
                        try
                        {
                            rsa.ImportRSAPrivateKey(Convert.FromBase64String(rsaKeyPair.PrivateKey), out _);
                        }
                        catch (Exception ex2)
                        {
                            _logger.LogWarning(ex2, "Failed to import RSA keys for client {ClientId}", clientId);
                            return null;
                        }
                    }

                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        ValidateIssuer = true,
                        ValidIssuer = GetIssuer(),
                        ValidateAudience = false, // We'll validate audience manually later
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                    };

                    var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                    var subjectClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub) ?? 
                                     principal.FindFirst("sub") ?? 
                                     principal.FindFirst(ClaimTypes.NameIdentifier);

                    return subjectClaim?.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        private static string GetIssuer()
        {
            // 実際の実装では設定ファイルから取得
            return "https://ecauth.example.com";
        }

        private async Task<string> GetClientIdStringAsync(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            return client?.ClientId ?? clientId.ToString();
        }
    }
}