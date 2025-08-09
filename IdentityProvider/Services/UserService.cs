using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Services
{
    public class UserService : IUserService
    {
        private readonly EcAuthDbContext _context;

        public UserService(EcAuthDbContext context)
        {
            _context = context;
        }

        public async Task<EcAuthUser> GetOrCreateUserAsync(IUserService.UserCreationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ExternalProvider))
                throw new ArgumentException("ExternalProvider cannot be null or empty.", nameof(request.ExternalProvider));
            if (string.IsNullOrWhiteSpace(request.ExternalSubject))
                throw new ArgumentException("ExternalSubject cannot be null or empty.", nameof(request.ExternalSubject));
            if (string.IsNullOrWhiteSpace(request.EmailHash))
                throw new ArgumentException("EmailHash cannot be null or empty.", nameof(request.EmailHash));
            if (request.OrganizationId <= 0)
                throw new ArgumentException("OrganizationId must be positive.", nameof(request.OrganizationId));

            // 既存ユーザーの検索（外部IdP情報で検索）
            var existingUser = await GetUserByExternalIdAsync(
                request.ExternalProvider, 
                request.ExternalSubject, 
                request.OrganizationId);

            if (existingUser != null)
            {
                // ユーザーが既存の場合、EmailHashを更新（変更されている可能性があるため）
                if (existingUser.EmailHash != request.EmailHash)
                {
                    existingUser.EmailHash = request.EmailHash;
                    existingUser.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return existingUser;
            }

            // 新規ユーザーの作成（JITプロビジョニング）
            var newUser = new EcAuthUser
            {
                Subject = Guid.NewGuid().ToString(),
                EmailHash = request.EmailHash,
                OrganizationId = request.OrganizationId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.EcAuthUsers.Add(newUser);

            // 外部IdPマッピングの作成
            var externalMapping = new ExternalIdpMapping
            {
                EcAuthSubject = newUser.Subject,
                ExternalProvider = request.ExternalProvider,
                ExternalSubject = request.ExternalSubject,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.ExternalIdpMappings.Add(externalMapping);

            await _context.SaveChangesAsync();

            return newUser;
        }

        public async Task<EcAuthUser?> GetUserBySubjectAsync(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return null;

            return await _context.EcAuthUsers
                .Include(u => u.Organization)
                .Include(u => u.ExternalIdpMappings)
                .FirstOrDefaultAsync(u => u.Subject == subject);
        }

        public async Task<EcAuthUser?> GetUserByExternalIdAsync(string externalProvider, string externalSubject, int organizationId)
        {
            if (string.IsNullOrWhiteSpace(externalProvider) || string.IsNullOrWhiteSpace(externalSubject))
                return null;

            var mapping = await _context.ExternalIdpMappings
                .Include(m => m.EcAuthUser)
                .ThenInclude(u => u!.Organization)
                .Include(m => m.EcAuthUser)
                .ThenInclude(u => u!.ExternalIdpMappings)
                .FirstOrDefaultAsync(m => 
                    m.ExternalProvider == externalProvider && 
                    m.ExternalSubject == externalSubject &&
                    m.EcAuthUser!.OrganizationId == organizationId);

            return mapping?.EcAuthUser;
        }
    }
}