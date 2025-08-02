using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Services
{
    public class AuthorizationCodeService : IAuthorizationCodeService
    {
        private readonly EcAuthDbContext _context;
        private readonly ILogger<AuthorizationCodeService> _logger;
        private const int CODE_EXPIRATION_MINUTES = 10; // OAuth2 spec recommends maximum of 10 minutes

        public AuthorizationCodeService(EcAuthDbContext context, ILogger<AuthorizationCodeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateAuthorizationCodeAsync(
            int clientId, 
            int ecAuthUserId, 
            string? redirectUri = null, 
            string? scope = null)
        {
            var code = Guid.NewGuid().ToString("N"); // Remove hyphens for cleaner code
            
            var authCode = new AuthorizationCode
            {
                Code = code,
                ClientId = clientId,
                EcAuthUserId = ecAuthUserId,
                RedirectUri = redirectUri,
                Scope = scope,
                ExpiresAt = DateTimeOffset.Now.AddMinutes(CODE_EXPIRATION_MINUTES),
                Used = false,
                CreatedAt = DateTimeOffset.Now
            };

            _context.AuthorizationCodes.Add(authCode);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated authorization code for client {ClientId} and user {UserId}", 
                clientId, ecAuthUserId);

            return code;
        }

        public async Task<AuthorizationCode?> ValidateAuthorizationCodeAsync(string code, string clientId)
        {
            var authCode = await _context.AuthorizationCodes
                .Include(ac => ac.Client)
                .Include(ac => ac.EcAuthUser)
                    .ThenInclude(u => u.Organization)
                .FirstOrDefaultAsync(ac => ac.Code == code && ac.Client.ClientId == clientId);

            if (authCode == null)
            {
                _logger.LogWarning("Authorization code not found for code {Code} and client {ClientId}", 
                    code, clientId);
                return null;
            }

            if (authCode.Used)
            {
                _logger.LogWarning("Authorization code {Code} has already been used", code);
                return null;
            }

            if (authCode.ExpiresAt < DateTimeOffset.Now)
            {
                _logger.LogWarning("Authorization code {Code} has expired", code);
                return null;
            }

            return authCode;
        }

        public async Task MarkCodeAsUsedAsync(int codeId)
        {
            var authCode = await _context.AuthorizationCodes.FindAsync(codeId);
            if (authCode != null)
            {
                authCode.Used = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Marked authorization code {CodeId} as used", codeId);
            }
        }
    }
}