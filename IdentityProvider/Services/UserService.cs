using System.Security.Cryptography;
using System.Text;
using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Services
{
    public class UserService : IUserService
    {
        private readonly EcAuthDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(EcAuthDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EcAuthUser> GetOrCreateUserAsync(UserCreationRequest request)
        {
            // まず外部IdPのsubjectで既存ユーザーを検索
            var existingUser = await GetUserByExternalProviderAsync(request.ExternalProvider, request.ExternalSubject);
            if (existingUser != null)
            {
                _logger.LogInformation("Existing user found for {Provider}/{Subject}", request.ExternalProvider, request.ExternalSubject);
                return existingUser;
            }

            // 新規ユーザー作成（JITプロビジョニング）
            var ecAuthSubject = Guid.NewGuid().ToString();
            var emailHash = !string.IsNullOrEmpty(request.Email) ? HashEmail(request.Email) : null;

            var newUser = new EcAuthUser
            {
                Subject = ecAuthSubject,
                EmailHash = emailHash,
                OrganizationId = request.OrganizationId,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now
            };

            _context.EcAuthUsers.Add(newUser);
            await _context.SaveChangesAsync();

            // 外部IdPとのマッピングを作成
            var mapping = new ExternalIdpMapping
            {
                EcAuthUserId = newUser.Id,
                ExternalProvider = request.ExternalProvider,
                ExternalSubject = request.ExternalSubject,
                CreatedAt = DateTimeOffset.Now
            };

            _context.ExternalIdpMappings.Add(mapping);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user created with subject {Subject} for {Provider}/{ExternalSubject}", 
                ecAuthSubject, request.ExternalProvider, request.ExternalSubject);

            return newUser;
        }

        public async Task<EcAuthUser?> GetUserBySubjectAsync(string subject)
        {
            return await _context.EcAuthUsers
                .Include(u => u.Organization)
                .Include(u => u.ExternalIdpMappings)
                .FirstOrDefaultAsync(u => u.Subject == subject);
        }

        public async Task<EcAuthUser?> GetUserByExternalProviderAsync(string provider, string externalSubject)
        {
            var mapping = await _context.ExternalIdpMappings
                .Include(m => m.EcAuthUser)
                    .ThenInclude(u => u.Organization)
                .FirstOrDefaultAsync(m => m.ExternalProvider == provider && m.ExternalSubject == externalSubject);

            return mapping?.EcAuthUser;
        }

        public string HashEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            // 小文字に正規化してからハッシュ化
            var normalizedEmail = email.Trim().ToLowerInvariant();
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedEmail));
            return Convert.ToBase64String(hashBytes);
        }
    }
}